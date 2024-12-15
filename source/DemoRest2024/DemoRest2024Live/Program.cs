/*
 dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools

dotnet tool install --global dotnet-ef

dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package SharpGrip.FluentValidation.AutoValidation.Endpoints


// Microsoft.AspNetCore.Identity
// Microsoft.AspNetCore.Identity.EntityFrameworkCore
// Microsoft.AspNetCore.Authentication.JwtBearer

*/

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DemoRest2024Live;
using DemoRest2024Live.Auth;
using DemoRest2024Live.Auth.Model;
using DemoRest2024Live.Data;
using DemoRest2024Live.Data.Entities;
using DemoRest2024Live.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using SharpGrip.FluentValidation.AutoValidation.Shared.Extensions;

//šitus buvau pridėjęs darydamas log out. Galimai nereikalingi.
//using Microsoft.IdentityModel.JsonWebTokens;
//using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        //kai susideployinsiu fronta sita tiesiog pasikeist 42:10 deployment video.
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation(configuration =>
{
    configuration.OverrideDefaultResultFactoryWith<ProblemDetailsResultFactory>();
});
builder.Services.AddResponseCaching();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddTransient<SessionService>();
builder.Services.AddScoped<AuthSeeder>();

builder.Services.AddIdentity<BarberShopClient, IdentityRole>()
    .AddEntityFrameworkStores<ForumDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(configureOptions: options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]));
});

builder.Services.AddAuthorization();


var app = builder.Build();
app.UseCors();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
//dbContext.Database.Migrate(); pagooglint ar reikia
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthSeeder>();
await dbSeeder.SeedAsync();
/*
    /api/v1/service GET List 200
    /api/v1/service POST Create 201
    /api/v1/service/{id} GET One 200
    /api/v1/service/{id} PUT/PATCH Modify 200
    /api/v1/service/{id} DELETE Remove 200/204
 */

// app.AddTopicApi();

app.AddAuthApi();

app.MapGet("api", (HttpContext httpContext, LinkGenerator linkGenerator) => Results.Ok(new List<LinkDto>
{
    new(linkGenerator.GetUriByName(httpContext, "GetServices"), "services", "GET"),
    new(linkGenerator.GetUriByName(httpContext, "CreateService"), "createService", "POST"),
    new(linkGenerator.GetUriByName(httpContext, "GetRoot"), "self", "GET"),
})).WithName("GetRoot");

var servicesGroups = app.MapGroup("/api").AddFluentValidationAutoValidation();

// /api/v1/topics?pageSize=5&pageNumber=1

servicesGroups.MapGet("/services", async ([AsParameters] SearchParameters searchParams, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
   // var queryable = dbContext.Services.AsQueryable().OrderBy(o => o.Price);
    var queryable = dbContext.Services
    .Where(s => !s.IsDeleted) // Filter out services with IsDeleted = true
    .OrderBy(o => o.Price)
    .AsQueryable();

    var pagedList = await PagedList<Service>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

    var resources = pagedList.Select(service =>
    {
        var links = CreateLinksForSingleTopic(service.Id, linkGenerator, httpContext).ToArray();
        return new ResourceDto<ServiceDto>(service.ToDto(), links);
    }).ToArray();

    var links = CreateLinksForTopics(linkGenerator, httpContext,
        pagedList.GetPreviousPageLink(linkGenerator, httpContext, "GetServices"),
        pagedList.GetNextPageLink(linkGenerator, httpContext, "GetServices")).ToArray();
    
    var paginationMetadata = pagedList.CreatePaginationMetadata(linkGenerator, httpContext, "GetServices");
    httpContext.Response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationMetadata));

    return new ResourceDto<ResourceDto<ServiceDto>[]>(resources, links);
}).WithName("GetServices");

servicesGroups.MapGet("/services/{serviceId}", [Authorize] async (int serviceId, HttpContext httpContext, ForumDbContext dbContext) =>
//servicesGroups.MapGet("/services/{serviceId}", async (int serviceId, ForumDbContext dbContext) =>
{
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
    httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != service.BarberShopClientID)
    {
        //NotFound()
        return Results.Forbid();
    }
    return service == null ? Results.NotFound() : TypedResults.Ok(service.ToDto());
}).WithName("GetService").AddEndpointFilter<ETagFilter>();

servicesGroups.MapPost("/services", [Authorize(Roles = BarberShopRoles.BarberShopTeacher)]
async (CreateServiceDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapPost("/services", [Authorize(Roles = BarberShopRoles.BarberShopClient)]
//async (CreateServiceDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
//{
    var service = new Service { Name = dto.Name, Price = dto.Price, BarberShopClientID = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) };
    dbContext.Services.Add(service);
    
    await dbContext.SaveChangesAsync();

    var links = CreateLinksForSingleTopic(service.Id, linkGenerator, httpContext).ToArray();
    var serviceDto = service.ToDto();
    var resource = new ResourceDto<ServiceDto>(serviceDto, links);
    
    return TypedResults.Created(links[0].Href, resource);
}).WithName("CreateService");

servicesGroups.MapPut("/services/{serviceId}", [Authorize] async (UpdateServiceDto dto, int serviceId, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    if(!httpContext.User.IsInRole(BarberShopRoles.Admin) && 
        httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != service.BarberShopClientID)
    {
        //NotFound()
        return Results.Forbid();
    }

    service.Price = dto.Price;
    dbContext.Services.Update(service);
    await dbContext.SaveChangesAsync();

    return Results.Ok(service.ToDto());
}).WithName("UpdateService");




//servicesGroups.MapDelete("/services/{serviceId}", async (int serviceId, ForumDbContext dbContext) =>
//{
//    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
//    if (service == null)
//    {
//        return Results.NotFound();
//    }

//    // Mark the service as deleted instead of removing it
//    service.IsDeleted = true;
//    dbContext.Services.Update(service);
//    await dbContext.SaveChangesAsync();

//    // Return the updated service indicating it was marked as deleted
//    return Results.Ok(service.ToDto());
//}).WithName("RemoveService");

servicesGroups.MapDelete("/services/{serviceId}", [Authorize] async (int serviceId, HttpContext httpContext, ForumDbContext dbContext) =>
//servicesGroups.MapDelete("/services/{serviceId}", async (int serviceId, ForumDbContext dbContext) =>
{
    // Retrieve the service with the given ID and check if it is already deleted
    var service = await dbContext.Services
        .Include(s => s.Reservations) // Include related reservations
        .ThenInclude(r => r.Reviews) // Include related reviews for each reservation
        .FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);

    if (service == null)
    {
        return Results.NotFound();
    }

    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
    httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != service.BarberShopClientID)
    {
        //NotFound()
        return Results.Forbid();
    }

    // Mark the service as deleted
    service.IsDeleted = true;

    // Mark all related reservations as deleted
    foreach (var reservation in service.Reservations)
    {
        reservation.IsDeleted = true;

        // Mark all reviews related to this reservation as deleted
        foreach (var review in reservation.Reviews)
        {
            review.IsDeleted = true;
            dbContext.Reviews.Update(review); // Explicitly update the review state
        }

        dbContext.Reservations.Update(reservation); // Explicitly update the reservation state
    }

    dbContext.Services.Update(service); // Update the service state
    await dbContext.SaveChangesAsync();

    // Return the updated service indicating it and all its nested entities were marked as deleted
    return Results.Ok(service.ToDto());
}).WithName("RemoveService");


//-------------------------------------------------------------------------------------------------------------------------------------
// Add reservation-related endpoints
servicesGroups.MapGet("/services/{serviceId}/reservations", async (int serviceId, [AsParameters] SearchParameters searchParams, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
    // Filter reservations by the associated serviceId, not reservation Id
    var queryable = dbContext.Reservations
        .Where(r => r.ServiceId == serviceId && !r.IsDeleted) // Filter by ServiceId and exclude deleted reservations
        .AsQueryable()
        .OrderBy(o => o.Date);

    var pagedList = await PagedList<Reservation>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

    var resources = pagedList.Select(reservation =>
    {
        var links = CreateLinksForSingleReservation(reservation.Id, serviceId, linkGenerator, httpContext).ToArray();
        return new ResourceDto<ReservationDto>(reservation.ToDto(), links);
    }).ToArray();

    var links = CreateLinksForReservations(linkGenerator, httpContext,
        pagedList.GetPreviousPageLink(linkGenerator, httpContext, "GetReservations", new { serviceId }),
        pagedList.GetNextPageLink(linkGenerator, httpContext, "GetReservations", new { serviceId }), serviceId).ToArray();

    var paginationMetadata = pagedList.CreatePaginationMetadata(linkGenerator, httpContext, "GetReservations", new { serviceId });
    httpContext.Response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationMetadata));

    return new ResourceDto<ResourceDto<ReservationDto>[]>(resources, links);
}).WithName("GetReservations");

servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
{
    var reservation = await dbContext.Reservations
        .FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId && !r.IsDeleted);

    return reservation == null
        ? Results.NotFound()
        : TypedResults.Ok(reservation.ToDto());
}).WithName("GetReservation").AddEndpointFilter<ETagFilter>();

servicesGroups.MapPost("/services/{serviceId}/reservations", [Authorize(Roles = BarberShopRoles.BarberShopClient)]
async (int serviceId, CreateReservationDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapPost("/services/{serviceId}/reservations", async (int serviceId, CreateReservationDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
//{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }
    
    
    //var reservation = new Reservation { ServiceId = serviceId, Date = dto.Date, BarberShopClientID = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) };

    // Create a new reservation associated with the given serviceId
    var reservation = new Reservation { ServiceId = serviceId, Date = dto.Date, BarberShopClientID = "" };
    dbContext.Reservations.Add(reservation);

    await dbContext.SaveChangesAsync();

    // Generate HATEOAS links for the created reservation
    var links = CreateLinksForSingleReservation(reservation.Id, serviceId, linkGenerator, httpContext).ToArray();
    var reservationDto = reservation.ToDto();
    var resource = new ResourceDto<ReservationDto>(reservationDto, links);

    // Return Created (201) response with the resource
    return TypedResults.Created(links[0].Href, resource);
}).WithName("CreateReservation");

servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}", [Authorize]
async (UpdateReservationDto dto, int serviceId, int reservationId, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}", async (UpdateReservationDto dto, int serviceId, int reservationId, ForumDbContext dbContext) =>
//{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId && !r.IsDeleted);
    if (reservation == null)
    {
        return Results.NotFound();
    }

    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
        !httpContext.User.IsInRole(BarberShopRoles.BarberShopTeacher) &&
        httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != reservation.BarberShopClientID)
    {
        return Results.Forbid();
    }

        if (!Enum.TryParse<ReservationStatus>(dto.Status, true, out var status))
    {
        return Results.BadRequest("Invalid status value.");
    }

    reservation.Status = status;
    dbContext.Reservations.Update(reservation);
    await dbContext.SaveChangesAsync();

    return Results.Ok(reservation.ToDto());
}).WithName("UpdateReservation");


//servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
//{
//    var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId && !r.IsDeleted);
//    if (reservation == null)
//    {
//        return Results.NotFound();
//    }

//    // Mark the reservation as deleted instead of removing it
//    reservation.IsDeleted = true;
//    dbContext.Reservations.Update(reservation);
//    await dbContext.SaveChangesAsync();

//    // Return the updated reservation indicating it was marked as deleted
//    return Results.Ok(reservation.ToDto());
//}).WithName("RemoveReservation");

servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}", [Authorize] async (int serviceId, int reservationId, HttpContext httpContext, ForumDbContext dbContext) =>
//servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    // Find the reservation and ensure it's not already deleted
    var reservation = await dbContext.Reservations
        .Include(r => r.Reviews) // Include related reviews
        .FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId && !r.IsDeleted);

    if (reservation == null)
    {
        return Results.NotFound();
    }

    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
    httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != reservation.BarberShopClientID)
    {
        //NotFound()
        return Results.Forbid();
    }

    // Mark the reservation as deleted instead of removing it
    reservation.IsDeleted = true;

    // Mark all related reviews as deleted
    foreach (var review in reservation.Reviews)
    {
        review.IsDeleted = true;
        dbContext.Reviews.Update(review); // Update the state of each review to reflect the change
    }

    dbContext.Reservations.Update(reservation); // Update the state of the reservation
    await dbContext.SaveChangesAsync();

    // Return the updated reservation indicating it and its reviews were marked as deleted
    return Results.Ok(reservation.ToDto());
}).WithName("RemoveReservation");


//-------------------------------------------------------------------------------------------------------------------------------------
// Add review-related endpoints
servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}/reviews", async (int serviceId, int reservationId, [AsParameters] SearchParameters searchParams, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
    // Ensure that the reviews are for the correct reservation and service
    var queryable = dbContext.Reviews
        .Where(r => r.ReservationId == reservationId && !r.IsDeleted)
        .AsQueryable()
        .OrderBy(o => o.Rating);

    var pagedList = await PagedList<Review>.CreateAsync(queryable, searchParams.PageNumber!.Value, searchParams.PageSize!.Value);

    var resources = pagedList.Select(review =>
    {
        var links = CreateLinksForSingleReview(review.Id, serviceId, reservationId, linkGenerator, httpContext).ToArray();
        return new ResourceDto<ReviewDto>(review.ToDto(), links);
    }).ToArray();

    var links = CreateLinksForReviews(linkGenerator, httpContext,
        pagedList.GetPreviousPageLink(linkGenerator, httpContext, "GetReviews", new { serviceId, reservationId }),
        pagedList.GetNextPageLink(linkGenerator, httpContext, "GetReviews", new { serviceId, reservationId }), serviceId, reservationId).ToArray();

    var paginationMetadata = pagedList.CreatePaginationMetadata(linkGenerator, httpContext, "GetReviews", new { serviceId, reservationId });
    httpContext.Response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationMetadata));

    return new ResourceDto<ResourceDto<ReviewDto>[]>(resources, links);
}).WithName("GetReviews");

servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}",
    async (int serviceId, int reservationId, int reviewId, HttpContext httpContext, ForumDbContext dbContext) =>
    {
    //servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (int serviceId, int reservationId, int reviewId, ForumDbContext dbContext) =>
//{
    var review = await dbContext.Reviews
        .FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId && !r.IsDeleted);

    return review == null
        ? Results.NotFound()
        : TypedResults.Ok(review.ToDto());
}).WithName("GetReview").AddEndpointFilter<ETagFilter>();

servicesGroups.MapPost("/services/{serviceId}/reservations/{reservationId}/reviews", [Authorize(Roles = BarberShopRoles.BarberShopClient)]
async (int serviceId, int reservationId, CreateReviewDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapPost("/services/{serviceId}/reservations/{reservationId}/reviews", async (int serviceId, int reservationId, CreateReviewDto dto, LinkGenerator linkGenerator, HttpContext httpContext, ForumDbContext dbContext) =>
//{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    // Check if the service or reservation is deleted
    var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);
    if (reservation == null)
    {
        return Results.NotFound();
    }

    // Create a new review and associate it with the correct reservation
    var review = new Review { ReservationId = reservationId, Content = dto.Content, Rating = dto.Rating, BarberShopClientID = "" };
    //var review = new Review { ReservationId = reservationId, Content = dto.Content, Rating = dto.Rating, BarberShopClientID = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) };
    dbContext.Reviews.Add(review);

    await dbContext.SaveChangesAsync();

    // Generate HATEOAS links for the created review
    var links = CreateLinksForSingleReview(review.Id, serviceId, reservationId, linkGenerator, httpContext).ToArray();
    var reviewDto = review.ToDto();
    var resource = new ResourceDto<ReviewDto>(reviewDto, links);

    return TypedResults.Created(links[0].Href, resource);
}).WithName("CreateReview");


servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", [Authorize]
async (UpdateReviewDto dto, int serviceId, int reservationId, int reviewId, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (UpdateReviewDto dto, int serviceId, int reservationId, int reviewId, ForumDbContext dbContext) =>
//{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    // Check if the service or reservation is deleted
    var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);
    if (reservation == null)
    {
        return Results.NotFound();
    }

    // Find the review and ensure it is linked to the correct reservation and service
    var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId && !r.IsDeleted);
    if (review == null)
    {
        return Results.NotFound();
    }

    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
    httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != review.BarberShopClientID)
    {
        //NotFound()
        return Results.Forbid();
    }

    // Update the review content and rating
    review.Content = dto.Content;
    review.Rating = dto.Rating;

    dbContext.Reviews.Update(review);
    await dbContext.SaveChangesAsync();

    return Results.Ok(review.ToDto());
}).WithName("UpdateReview");

servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", [Authorize]
async (int serviceId, int reservationId, int reviewId, HttpContext httpContext, ForumDbContext dbContext) =>
{
//servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (int serviceId, int reservationId, int reviewId, ForumDbContext dbContext) =>
//{
    // Check if the service or reservation is deleted
    var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId && !s.IsDeleted);
    if (service == null)
    {
        return Results.NotFound();
    }

    // Check if the service or reservation is deleted
    var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted);
    if (reservation == null)
    {
        return Results.NotFound();
    }

    var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId && !r.IsDeleted);
    if (review == null)
    {
        return Results.NotFound();
    }

    if (!httpContext.User.IsInRole(BarberShopRoles.Admin) &&
        !httpContext.User.IsInRole(BarberShopRoles.BarberShopTeacher) &&
        httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != reservation.BarberShopClientID)
    {
        return Results.Forbid();
    }

    // Mark the review as deleted instead of removing it
    review.IsDeleted = true;
    dbContext.Reviews.Update(review);
    await dbContext.SaveChangesAsync();

    // Return the updated review indicating it was marked as deleted
    return Results.Ok(review.ToDto());
}).WithName("RemoveReview");

//-------------------------------------------------------------------------------------------------------------------------------------

app.UseRouting();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static IEnumerable<LinkDto> CreateLinksForSingleTopic(int serviceId, LinkGenerator linkGenerator, HttpContext httpContext)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetService", new { serviceId }), "self", "GET");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "UpdateService", new { serviceId }), "edit", "PUT");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "RemoveService", new { serviceId }), "remove", "DELETE");
}

static IEnumerable<LinkDto> CreateLinksForTopics(LinkGenerator linkGenerator, HttpContext httpContext, string? previousPageLink, string? nextPageLink)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetServices"), "self", "GET");
    
    if(previousPageLink != null)
        yield return new LinkDto(previousPageLink, "previousPage", "GET");
    
    if(nextPageLink != null)
        yield return new LinkDto(nextPageLink, "nextPage", "GET");
}


//-------------------------------------------------------------------------------------------------------------------------------------
static IEnumerable<LinkDto> CreateLinksForSingleReservation(int reservationId, int serviceId, LinkGenerator linkGenerator, HttpContext httpContext)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetReservation", new { serviceId, reservationId }), "self", "GET");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "UpdateReservation", new { serviceId, reservationId }), "edit", "PUT");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "RemoveReservation", new { serviceId, reservationId }), "remove", "DELETE");
}

static IEnumerable<LinkDto> CreateLinksForReservations(LinkGenerator linkGenerator, HttpContext httpContext, string? previousPageLink, string? nextPageLink, int serviceId)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetReservations", new { serviceId }), "self", "GET");

    if (previousPageLink != null)
        yield return new LinkDto(previousPageLink, "previousPage", "GET");

    if (nextPageLink != null)
        yield return new LinkDto(nextPageLink, "nextPage", "GET");
}

//-------------------------------------------------------------------------------------------------------------------------------------
static IEnumerable<LinkDto> CreateLinksForSingleReview(int reviewId, int serviceId, int reservationId, LinkGenerator linkGenerator, HttpContext httpContext)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetReview", new { serviceId, reservationId, reviewId }), "self", "GET");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "UpdateReview", new { serviceId, reservationId, reviewId }), "edit", "PUT");
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "RemoveReview", new { serviceId, reservationId, reviewId }), "remove", "DELETE");
}

static IEnumerable<LinkDto> CreateLinksForReviews(LinkGenerator linkGenerator, HttpContext httpContext, string? previousPageLink, string? nextPageLink, int serviceId, int reservationId)
{
    yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetReviews", new { serviceId, reservationId }), "self", "GET");

    if (previousPageLink != null)
        yield return new LinkDto(previousPageLink, "previousPage", "GET");

    if (nextPageLink != null)
        yield return new LinkDto(nextPageLink, "nextPage", "GET");
}


//-------------------------------------------------------------------------------------------------------------------------------------




//As uzkomentavau
//public record UpdateCommentParameters(int serviceId, int reservationId, int reviewtId, UpdateCommentDto dto, ForumDbContext dbContext);
//public record UpdateCommentDto(string Content);
//public record UpdatePostDto(string Body);

public class ProblemDetailsResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, ValidationResult validationResult)
    {
        var problemDetails = new HttpValidationProblemDetails(validationResult.ToValidationProblemErrors())
        {
            Type =  "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Unprocessable Entity",
            Status = 422
        };
        
        return TypedResults.Problem(problemDetails);
    }
}

public record ServiceDto(int Id, string Name, decimal Price, bool IsDeleted);

public record CreateServiceDto(string Name, decimal Price)
{
    public class CreateServiceDtoValidator : AbstractValidator<CreateServiceDto>
    {
        public CreateServiceDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(min: 2, max: 100);
            RuleFor(x => x.Price)
                .NotEmpty()
                .GreaterThan(0).WithMessage("Price must be greater than zero.");
        }
    }
};


public record UpdateServiceDto(decimal Price)
{
    public class UpdateServiceDtoValidator : AbstractValidator<UpdateServiceDto>
    {
        public UpdateServiceDtoValidator()
        {
            RuleFor(x => x.Price)
                .NotEmpty()
                .GreaterThan(0).WithMessage("Price must be greater than zero.");
        }
    }
};



//-------------------------------------------------------------------------------------------------------------------------------------

public record ReservationDto(int Id, DateTimeOffset Date, ReservationStatus Status, bool IsDeleted, int ServiceId);

public record CreateReservationDto(DateTimeOffset Date)
{
    public DateTimeOffset Date { get; init; } = Date.ToUniversalTime(); // Automatically convert to UTC

    public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
    {
        public CreateReservationDtoValidator()
        {
            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(date => date > DateTimeOffset.UtcNow).WithMessage("Reservation date must be in the future.");
        }
    }
};

public record UpdateReservationDto(string Status)
{
    public class UpdateReservationDtoValidator : AbstractValidator<UpdateReservationDto>
    {
        public UpdateReservationDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(status => Enum.TryParse<ReservationStatus>(status, true, out _)).WithMessage("Invalid reservation status.");
        }
    }
}


//-------------------------------------------------------------------------------------------------------------------------------------


public record ReviewDto(int Id, string Content, int Rating, bool IsDeleted, int ReservationId);

public record CreateReviewDto(string Content, int Rating)
{
    public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
    {
        public CreateReviewDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .Length(min: 1, max: 500).WithMessage("Content must be between 1 and 500 characters.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        }
    }
};

public record UpdateReviewDto(string Content, int Rating)
{
    public class UpdateReviewDtoValidator : AbstractValidator<UpdateReviewDto>
    {
        public UpdateReviewDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required.")
                .Length(min: 1, max: 500).WithMessage("Content must be between 1 and 500 characters.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        }
    }
};

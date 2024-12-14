using DemoRest2024Live.Data;
using DemoRest2024Live.Data.Entities;
using Microsoft.EntityFrameworkCore;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace DemoRest2024Live;

public static class Endpoints
{
    public static void AddServiceApi(this WebApplication app)
    {
        var servicesGroups = app.MapGroup("/api").AddFluentValidationAutoValidation();

        //    servicesGroups.MapGet("/services", async (ForumDbContext dbContext) =>
        //    {
        //        return (await dbContext.Services.ToListAsync()).Select(service => service.ToDto());
        //    });
        //    servicesGroups.MapGet("/services/{serviceId}", async (int serviceId, ForumDbContext dbContext) =>
        //    {
        //        var service = await dbContext.Services.FindAsync(serviceId);
        //        return service == null ? Results.NotFound() : TypedResults.Ok(service.ToDto());
        //    });
        //    //servicesGroups.MapPost("/services", async (CreateServiceDto dto, ForumDbContext dbContext) =>
        //    //{
        //    //    var service = new Service { Name = dto.Name, Price = dto.Price };
        //    //    dbContext.Services.Add(service);

        //    //    await dbContext.SaveChangesAsync();

        //    //    return TypedResults.Created($"api/services/{service.Id}", service.ToDto());
        //    //});
        //    servicesGroups.MapPut("/services/{serviceId}", async (UpdateServiceDto dto, int serviceId, ForumDbContext dbContext) =>
        //    {
        //        var service = await dbContext.Services.FindAsync(serviceId);
        //        if (service == null)
        //        {
        //            return Results.NotFound();
        //        }

        //        service.Price = dto.Price;

        //        dbContext.Services.Update(service);
        //        await dbContext.SaveChangesAsync();

        //        return Results.Ok(service.ToDto());
        //    });
        //    servicesGroups.MapDelete("/services/{serviceId}", async (int serviceId, ForumDbContext dbContext) =>
        //    {
        //        var service = await dbContext.Services.FindAsync(serviceId);
        //        if (service == null)
        //        {
        //            return Results.NotFound();
        //        }

        //        dbContext.Services.Remove(service);
        //        await dbContext.SaveChangesAsync();

        //        return Results.NoContent();
        //    });


        //    // Reservation-related Endpoints

        //    servicesGroups.MapGet("/services/{serviceId}/reservations", async (int serviceId, ForumDbContext dbContext) =>
        //    {
        //        var reservations = await dbContext.Reservations
        //                                           .Where(r => r.ServiceId == serviceId)  // Filter by ServiceId
        //                                           .ToListAsync();
        //        if (reservations == null || !reservations.Any())
        //        {
        //            return Results.NotFound("No reservations found for the specified service.");
        //        }

        //        return Results.Ok(reservations.Select(r => r.ToDto()));
        //    }).Produces<IEnumerable<ReservationDto>>(200).Produces(404);


        //    // Create a reservation for a service
        //    //servicesGroups.MapPost("/services/{serviceId}/reservations", async (int serviceId, CreateReservationDto dto, ForumDbContext dbContext) =>
        //    //{
        //    //    if (dto == null)
        //    //    {
        //    //        return Results.BadRequest("Invalid reservation data.");
        //    //    }

        //    //    var reservation = new Reservation { ServiceId = serviceId, Date = dto.Date };  // Set ServiceId
        //    //    dbContext.Reservations.Add(reservation);
        //    //    await dbContext.SaveChangesAsync();

        //    //    return Results.Created($"/api/services/{serviceId}/reservations/{reservation.Id}", reservation.ToDto());
        //    //}).Produces<ReservationDto>(201).Produces(400).Produces(422);

        //    servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
        //    {
        //        var reservation = await dbContext.Reservations
        //                                         .FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId);  // Ensure ServiceId matches
        //        if (reservation == null)
        //        {
        //            return Results.NotFound("Reservation not found.");
        //        }

        //        return Results.Ok(reservation.ToDto());
        //    }).Produces<ReservationDto>(200).Produces(404);




        //    servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, UpdateReservationDto dto, ForumDbContext dbContext) =>
        //    {
        //        // Find the reservation with matching serviceId and reservationId
        //        var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId);
        //        if (reservation == null)
        //        {
        //            return Results.NotFound("Reservation not found.");
        //        }

        //        // Try converting the string status to the ReservationStatus enum
        //        if (!Enum.TryParse<ReservationStatus>(dto.Status, true, out var status))
        //        {
        //            // Return 400 Bad Request if the status value is not a valid enum value
        //            return Results.BadRequest("Invalid status value.");
        //        }

        //        // Update the reservation status and save changes
        //        reservation.Status = status;
        //        dbContext.Reservations.Update(reservation);
        //        await dbContext.SaveChangesAsync();

        //        // Return the updated reservation as a DTO
        //        return Results.Ok(reservation.ToDto());
        //    }).Produces<ReservationDto>(200).Produces(400).Produces(404).Produces(422);


        //    // Delete reservation by ID
        //    servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
        //    {
        //        var reservation = await dbContext.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.ServiceId == serviceId);  // Ensure ServiceId matches
        //        if (reservation == null)
        //        {
        //            return Results.NotFound("Reservation not found.");
        //        }

        //        dbContext.Reservations.Remove(reservation);
        //        await dbContext.SaveChangesAsync();

        //        return Results.NoContent();
        //    }).Produces(204).Produces(404);




        //    // Review-related Endpoints

        //    // List reviews for a reservation
        //    servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}/reviews", async (int serviceId, int reservationId, ForumDbContext dbContext) =>
        //    {
        //        var reviews = await dbContext.Reviews
        //                                     .Where(r => r.ReservationId == reservationId)  // Filter by ReservationId
        //                                     .ToListAsync();
        //        if (reviews == null || !reviews.Any())
        //        {
        //            return Results.NotFound("No reviews found for the specified reservation.");
        //        }

        //        return Results.Ok(reviews.Select(r => r.ToDto()));
        //    }).Produces<IEnumerable<ReviewDto>>(200).Produces(404);


        //    // Create a review for a reservation
        //    //servicesGroups.MapPost("/services/{serviceId}/reservations/{reservationId}/reviews", async (int serviceId, int reservationId, CreateReviewDto dto, ForumDbContext dbContext) =>
        //    //{
        //    //    if (dto == null)
        //    //    {
        //    //        return Results.BadRequest("Invalid review data.");
        //    //    }

        //    //    var review = new Review { ReservationId = reservationId, Content = dto.Content, Rating = dto.Rating };  // Set ReservationId
        //    //    dbContext.Reviews.Add(review);
        //    //    await dbContext.SaveChangesAsync();

        //    //    return Results.Created($"/api/services/{serviceId}/reservations/{reservationId}/reviews/{review.Id}", review.ToDto());
        //    //}).Produces<ReviewDto>(201).Produces(400).Produces(422);

        //    servicesGroups.MapGet("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (int serviceId, int reservationId, int reviewId, ForumDbContext dbContext) =>
        //    {
        //        var review = await dbContext.Reviews
        //                                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId);  // Ensure ReservationId matches
        //        if (review == null)
        //        {
        //            return Results.NotFound("Review not found.");
        //        }

        //        return Results.Ok(review.ToDto());
        //    }).Produces<ReviewDto>(200).Produces(404);

        //    // Update a review by ID
        //    servicesGroups.MapPut("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (int serviceId, int reservationId, int reviewId, UpdateReviewDto dto, ForumDbContext dbContext) =>
        //    {
        //        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId);  // Ensure ReservationId matches
        //        if (review == null)
        //        {
        //            return Results.NotFound("Review not found.");
        //        }

        //        review.Content = dto.Content;
        //        review.Rating = dto.Rating;
        //        dbContext.Reviews.Update(review);
        //        await dbContext.SaveChangesAsync();

        //        return Results.Ok(review.ToDto());
        //    }).Produces<ReviewDto>(200).Produces(400).Produces(404).Produces(422);


        //    // Delete a review by ID
        //    servicesGroups.MapDelete("/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}", async (int serviceId, int reservationId, int reviewId, ForumDbContext dbContext) =>
        //    {
        //        var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ReservationId == reservationId);  // Ensure ReservationId matches
        //        if (review == null)
        //        {
        //            return Results.NotFound("Review not found.");
        //        }

        //        dbContext.Reviews.Remove(review);
        //        await dbContext.SaveChangesAsync();

        //        return Results.NoContent();
        //    }).Produces(204).Produces(404);

    }
}
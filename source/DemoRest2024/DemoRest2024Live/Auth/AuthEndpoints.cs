using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DemoRest2024Live.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DemoRest2024Live.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            // register
             //app.MapPost("api/accounts", async (UserManager<BarberShopClient> userManager, RegisterUserDto dto, string role) =>

            app.MapPost("api/accounts", [AllowAnonymous] async (UserManager<BarberShopClient> userManager, RegisterUserDto dto) =>
            {
                // check user exists
                var user = await userManager.FindByNameAsync(dto.UserName);
                if (user != null)
                    return Results.UnprocessableEntity("Username already taken");

                var newUser = new BarberShopClient()
                {
                    Email = dto.Email,
                    UserName = dto.UserName,
                };

                // TODO: wrap in transaction !!!!!!!!!!!!!!!!!!!!!IMPORTANT!!!!!!!!!!!!!!!!!!!!35:19
                var createUserResult = await userManager.CreateAsync(newUser, dto.Password);
                if (!createUserResult.Succeeded)
                    return Results.UnprocessableEntity();

                //if (!BarberShopRoles.All.Contains(role))
                //    return Results.BadRequest("Invalid role.");


                //await userManager.AddToRoleAsync(newUser, role);

                await userManager.AddToRoleAsync(newUser, BarberShopRoles.BarberShopClient);

                return Results.Created();
            });

            // login
            app.MapPost("api/login", [AllowAnonymous] async (UserManager<BarberShopClient> userManager, JwtTokenService jwtTokenService,
                SessionService sessionService, HttpContext httpContext, LoginDto dto) =>
            {
                // check user exists
                var user = await userManager.FindByNameAsync(dto.UserName);
                if (user == null)
                    return Results.UnprocessableEntity("User does not exist");

                var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
                if (!isPasswordValid)
                    return Results.UnprocessableEntity("Username or password was incorrect.");

                var roles = await userManager.GetRolesAsync(user);

                var sessionId = Guid.NewGuid(); //!!!!!!!!!!!Galima leist duombazei sugeneruot. Kadangi SImonas generuoja pats del to sitaip padare. 1:23:12
                var expiresAt = DateTime.UtcNow.AddDays(3);
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(sessionId, user.Id, expiresAt);

                await sessionService.CreateSessionAsync(sessionId, user.Id, refreshToken, expiresAt);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = expiresAt,
                    //Secure = false => should be true possibly
                };

                httpContext.Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

                return Results.Ok(new SuccessfulLoginDto(accessToken));
            });

            app.MapPost("api/accessToken", async (UserManager<BarberShopClient> userManager, JwtTokenService jwtTokenService, SessionService sessionService, HttpContext httpContext) =>
            {
                if (!httpContext.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                {
                    return Results.UnprocessableEntity();
                }

                if (!jwtTokenService.TryParseRefreshToken(refreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var sessionId = claims.FindFirstValue("SessionId");
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.UnprocessableEntity();
                }

                var sessionIdAsGuid = Guid.Parse(sessionId);
                if (!await sessionService.IsSessionValidAsync(sessionIdAsGuid, refreshToken))
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.UnprocessableEntity();
                }

                var roles = await userManager.GetRolesAsync(user);

                var expiresAt = DateTime.UtcNow.AddDays(3);
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var newRefreshToken = jwtTokenService.CreateRefreshToken(sessionIdAsGuid, user.Id, expiresAt);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = expiresAt,
                    //Secure = false => should be true possibly
                };

                httpContext.Response.Cookies.Append("RefreshToken", newRefreshToken, cookieOptions);

                await sessionService.ExtendSessionAsync(sessionIdAsGuid, newRefreshToken, expiresAt);

                return Results.Ok(new SuccessfulLoginDto(accessToken));
            });

            app.MapPost("api/logout", async (UserManager<BarberShopClient> userManager, JwtTokenService jwtTokenService,
                SessionService sessionService, HttpContext httpContext) =>
            {
                if (!httpContext.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                {
                    return Results.UnprocessableEntity();
                }

                if (!jwtTokenService.TryParseRefreshToken(refreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }

                var sessionId = claims.FindFirstValue("SessionId");
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.UnprocessableEntity();
                }

                await sessionService.InvalidateSessionAsync(Guid.Parse(sessionId));
                httpContext.Response.Cookies.Delete("RefreshToken");

                return Results.Ok();
            });

            //        app.MapPost("api/logout", async (UserManager<BarberShopClient> userManager, JwtTokenService jwtTokenService,
            //SessionService sessionService, ITokenBlacklistService tokenBlacklistService, HttpContext httpContext) =>
            //        {
            //            // 1. Retrieve Refresh Token from Cookies
            //            if (!httpContext.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            //            {
            //                return Results.UnprocessableEntity("Refresh token is missing.");
            //            }

            //            // 2. Parse the Refresh Token and Extract Claims
            //            if (!jwtTokenService.TryParseRefreshToken(refreshToken, out var claims))
            //            {
            //                return Results.UnprocessableEntity("Invalid refresh token.");
            //            }

            //            // 3. Extract and Validate Session ID
            //            var sessionId = claims.FindFirstValue("SessionId");
            //            if (string.IsNullOrWhiteSpace(sessionId))
            //            {
            //                return Results.UnprocessableEntity("Session ID is missing.");
            //            }

            //            // 4. Invalidate the Session
            //            await sessionService.InvalidateSessionAsync(Guid.Parse(sessionId));

            //            // 5. Blacklist the Access Token (Optional, if included in the Authorization header)
            //            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            //            {
            //                var accessToken = authorizationHeader.ToString().Split(" ").Last();
            //                if (jwtTokenService.TryParseAccessToken(accessToken, out var accessTokenClaims))
            //                {
            //                    var tokenId = accessTokenClaims.FindFirstValue(JwtRegisteredClaimNames.Jti);
            //                    if (!string.IsNullOrWhiteSpace(tokenId))
            //                    {
            //                        await tokenBlacklistService.AddToBlacklistAsync(tokenId);
            //                    }
            //                }
            //            }

            //            // 6. Remove Refresh Token Cookie
            //            httpContext.Response.Cookies.Delete("RefreshToken");

            //            // 7. Return Success
            //            return Results.Ok("Logged out successfully.");
            //        });



        }


        public record RegisterUserDto(string UserName, string Email, string Password);
        public record LoginDto(string UserName, string Password);
        public record SuccessfulLoginDto(string AccessToken);
    }
}

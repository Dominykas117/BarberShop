using System.IdentityModel.Tokens.Jwt;

namespace DemoRest2024Live.Auth
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public TokenValidationMiddleware(RequestDelegate next, ITokenBlacklistService tokenBlacklistService)
        {
            _next = next;
            _tokenBlacklistService = tokenBlacklistService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                var token = authorizationHeader.ToString().Split(" ").Last();
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtTokenHandler.ReadJwtToken(token);

                var tokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (tokenId != null && await _tokenBlacklistService.IsTokenBlacklistedAsync(tokenId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Access token is invalid or expired.");
                    return;
                }
            }

            await _next(context);
        }
    }

}

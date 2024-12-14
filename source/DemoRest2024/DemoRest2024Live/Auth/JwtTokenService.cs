using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DemoRest2024Live.Auth
{
    public class JwtTokenService
    {
        private readonly SymmetricSecurityKey _authSigningKey;
        private readonly string? _issuer;
        private readonly string? _audience;
        private readonly TokenValidationParameters _validationParameters;


        public JwtTokenService(IConfiguration configuration, TokenValidationParameters validationParameters)
        {
            _authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));
            _issuer = configuration["JWT:ValidIssuer"];
            _audience = configuration["JWT:ValidAudience"];
            _validationParameters = validationParameters;
        }

        public string CreateAccessToken(string userName, string userId, IEnumerable<string> roles)
        {
            var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
        };

            authClaims.AddRange(roles.Select(o => new Claim(ClaimTypes.Role, o)));

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                expires: DateTime.Now.AddMinutes(5), //sakė turėtų būti iki 10 min
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256) //čia minimaliausiai saugus algoritmas
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

         public string CreateRefreshToken(Guid sessionId, string userId, DateTime expires)
        {
            var authClaims = new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, userId),
                new("SessionId", sessionId.ToString())
            };

            var token = new JwtSecurityToken
            (
                issuer: _issuer,
                audience: _audience,
                expires: expires,
                claims: authClaims,
                signingCredentials: new SigningCredentials(_authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool TryParseRefreshToken(string refreshToken, out ClaimsPrincipal? claims)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler() { MapInboundClaims = false };
                var validationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _authSigningKey,
                    ValidateLifetime = true
                };

                claims = tokenHandler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch
            {
                claims = null;
                return false;
            }
        }

        public bool TryParseAccessToken(string token, out ClaimsPrincipal claims)
        {
            claims = null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Validate the token
                var principal = tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

                // Ensure the token is a valid JWT
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    claims = principal;
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary (optional)
                Console.WriteLine($"Failed to parse token: {ex.Message}");
            }

            return false;
        }
    }
}
using System.ComponentModel.DataAnnotations;
using DemoRest2024Live.Auth.Model;

namespace DemoRest2024Live.Data.Entities
{
    public class Session
    {
        public Guid Id { get; set; } // dar saugiau būtų kokia nors kriptografinė reikšmė 1:17:10

        public string LastRefreshToken { get; set; }

        public DateTimeOffset InitiatedAt { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }

        [Required]
        public required string UserId { get; set; }

        public BarberShopClient User { get; set; }
    }
}

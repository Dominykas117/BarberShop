using System.Collections.Concurrent;

namespace DemoRest2024Live.Auth
{
    public interface ITokenBlacklistService
    {
        Task AddToBlacklistAsync(string tokenId);
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
    }

    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

        public Task AddToBlacklistAsync(string tokenId)
        {
            // Add token to blacklist with an expiration timestamp
            _blacklist[tokenId] = DateTime.UtcNow.AddMinutes(15); // Adjust expiry duration based on access token lifespan
            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            // Check if the token is blacklisted and remove if expired
            if (_blacklist.TryGetValue(tokenId, out var expiry) && expiry > DateTime.UtcNow)
            {
                return Task.FromResult(true);
            }

            // Remove expired tokens
            _blacklist.TryRemove(tokenId, out _);
            return Task.FromResult(false);
        }
    }

}

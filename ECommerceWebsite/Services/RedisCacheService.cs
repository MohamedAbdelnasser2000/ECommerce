using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ECommerceWebsite.Services
{
    // Thin wrapper over IDistributedCache with JSON serialization and timeouts
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };

            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<T>(cached, serializerOptions);
            }

            var value = await factory();

            var options = new DistributedCacheEntryOptions();
            if (absoluteExpiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
            if (slidingExpiration.HasValue)
                options.SlidingExpiration = slidingExpiration;

            var payload = JsonSerializer.Serialize(value, serializerOptions);
            await _cache.SetStringAsync(key, payload, options);
            return value;
        }

        public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
    }
}
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ApiAggregation.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public T? Get<T>(string key)
        {
            try
            {
                // Attempt to retrieve the cached value; return default if it doesn't exist
                return _cache.TryGetValue(key, out T? value) ? value : default;
            }
            catch (Exception ex)
            {
                // Log error if there's an issue retrieving the item
                Log.Error(ex, "Cache retrieval failed for key: {Key}", key);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            // Define cache entry options with an absolute expiration
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            _cache.Set(key, value, cacheEntryOptions);
        }

    }
}

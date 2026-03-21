using System;
using Microsoft.Extensions.Caching.Memory;

namespace F1.Services
{
    public interface IGlobalMockDateService
    {
        DateTime? GetMockDateUtc();
        void SetMockDateUtc(DateTime? mockDateUtc);
    }

    public class GlobalMockDateService : IGlobalMockDateService
    {
        private const string CacheKey = "GlobalMockDateUtc";
        private readonly IMemoryCache _cache;

        public GlobalMockDateService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public DateTime? GetMockDateUtc()
        {
            return _cache.TryGetValue(CacheKey, out DateTime value) ? value : null;
        }

        public void SetMockDateUtc(DateTime? mockDateUtc)
        {
            if (mockDateUtc.HasValue)
            {
                _cache.Set(CacheKey, NormalizeToUtc(mockDateUtc.Value), TimeSpan.FromHours(12));
            }
            else
            {
                _cache.Remove(CacheKey);
            }
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}

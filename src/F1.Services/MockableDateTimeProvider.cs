using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using F1.Core.Interfaces;

namespace F1.Services
{
    public class MockableDateTimeProvider : IDateTimeProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IGlobalMockDateService _globalMockDateService;

        public MockableDateTimeProvider(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IGlobalMockDateService globalMockDateService)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _globalMockDateService = globalMockDateService;
        }

        public DateTime UtcNow
        {
            get
            {
                // 1. Check for HTTP header
                var headers = _httpContextAccessor.HttpContext?.Request?.Headers;
                if (headers != null && headers.TryGetValue("X-Mock-Date", out var mockDate))
                {
                    if (DateTime.TryParse(mockDate, out var parsedDate))
                    {
                        return parsedDate.ToUniversalTime();
                    }
                }
                // 2. Check for global mock date (cache/DB)
                var globalMockDate = _globalMockDateService.GetMockDateUtc();
                if (globalMockDate.HasValue)
                {
                    return globalMockDate.Value;
                }
                // 3. Fallback to real system time
                return DateTime.UtcNow;
            }
        }
    }
}

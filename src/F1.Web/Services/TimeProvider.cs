using System;

namespace F1.Web.Services
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class MockableTimeProvider : ITimeProvider
    {
        private readonly IMockDateService _mockDateService;
        public MockableTimeProvider(IMockDateService mockDateService)
        {
            _mockDateService = mockDateService;
        }
        public DateTime UtcNow => _mockDateService.GetMockDate() ?? DateTime.UtcNow;
    }
}

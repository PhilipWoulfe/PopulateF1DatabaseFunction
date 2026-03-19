using System;
using Microsoft.JSInterop;

namespace F1.Web.Services
{
    public interface IMockDateService
    {
        DateTime? GetMockDate();
        void SetMockDate(DateTime? date);
    }

    public class MockDateService : IMockDateService
    {
        private readonly IJSRuntime _js;
        private DateTime? _mockDate;
        private const string Key = "X-Mock-Date";

        public MockDateService(IJSRuntime js)
        {
            _js = js;
        }

        public DateTime? GetMockDate()
        {
            if (_mockDate.HasValue) return _mockDate;
            // In real impl, fetch from localStorage via JSInterop
            return null;
        }

        public void SetMockDate(DateTime? date)
        {
            _mockDate = date;
            // In real impl, set in localStorage via JSInterop
        }
    }
}

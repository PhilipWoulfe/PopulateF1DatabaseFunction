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
            // Synchronously calling async JSInterop is not supported; return null if not cached
            return null;
        }

        public async void SetMockDate(DateTime? date)
        {
            _mockDate = date;
            if (date.HasValue)
            {
                await SetInLocalStorageAsync(date.Value.ToString("o"));
            }
            else
            {
                await RemoveFromLocalStorageAsync();
            }
        }

        public async Task<DateTime?> GetMockDateAsync()
        {
            if (_mockDate.HasValue) return _mockDate;
            var dateStr = await GetFromLocalStorageAsync();
            if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            {
                _mockDate = dt;
                return dt;
            }
            return null;
        }

        private async Task<string?> GetFromLocalStorageAsync()
        {
            return await _js.InvokeAsync<string>("localStorage.getItem", Key);
        }

        private async Task SetInLocalStorageAsync(string value)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", Key, value);
        }

        private async Task RemoveFromLocalStorageAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", Key);
        }
    }
}

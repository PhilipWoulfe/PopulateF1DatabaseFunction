using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace F1.Web.Services
{
    public interface IMockDateService
    {
        DateTime? GetMockDate();
        Task RefreshAsync();
        Task SetMockDateAsync(DateTime? dateUtc);
    }

    public class MockDateService : IMockDateService
    {
        private readonly HttpClient _httpClient;
        private DateTime? _mockDateUtc;

        public MockDateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public DateTime? GetMockDate()
        {
            return _mockDateUtc;
        }

        public async Task RefreshAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync("admin/mock-date");
                if (!response.IsSuccessStatusCode)
                {
                    _mockDateUtc = null;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<MockDateResponse>();
                _mockDateUtc = result?.MockDate is DateTime mockDate
                    ? DateTime.SpecifyKind(mockDate, DateTimeKind.Utc)
                    : null;
            }
            catch (HttpRequestException)
            {
                _mockDateUtc = null;
            }
            catch (JsonException)
            {
                _mockDateUtc = null;
            }
        }

        public async Task SetMockDateAsync(DateTime? dateUtc)
        {
            var normalizedUtc = dateUtc.HasValue
                ? DateTime.SpecifyKind(dateUtc.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            using var response = await _httpClient.PostAsJsonAsync("admin/mock-date", new { mockDateUtc = normalizedUtc });
            response.EnsureSuccessStatusCode();
            _mockDateUtc = normalizedUtc;
        }
        
        private sealed class MockDateResponse
        {
            [JsonPropertyName("mockDate")]
            public DateTime? MockDate { get; set; }
        }
    }
}

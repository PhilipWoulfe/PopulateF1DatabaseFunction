using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using F1.Web.Services.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly ILogger<MockDateService> _logger;
        private DateTime? _mockDateUtc;

        public MockDateService(HttpClient httpClient, ILogger<MockDateService>? logger = null)
        {
            _httpClient = httpClient;
            _logger = logger ?? NullLogger<MockDateService>.Instance;
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
                    _logger.LogWarning("Mock date refresh returned non-success status code {StatusCode}.", (int)response.StatusCode);
                    _mockDateUtc = null;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<MockDateResponse>();
                _mockDateUtc = result?.MockDate is DateTime mockDate
                    ? DateTime.SpecifyKind(mockDate, DateTimeKind.Utc)
                    : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Mock date refresh failed due to HTTP request error.");
                _mockDateUtc = null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Mock date refresh failed due to JSON parse error.");
                _mockDateUtc = null;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Mock date refresh failed due to unsupported response content type.");
                _mockDateUtc = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Mock date refresh failed due to unexpected error.");
                _mockDateUtc = null;
            }
        }

        public async Task SetMockDateAsync(DateTime? dateUtc)
        {
            var normalizedUtc = dateUtc.HasValue
                ? DateTime.SpecifyKind(dateUtc.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            using var response = await _httpClient.PostAsJsonAsync("admin/mock-date", new { mockDateUtc = normalizedUtc });
            await ApiResponseParser.EnsureSuccessAsync(response, "Setting mock date");
            _mockDateUtc = normalizedUtc;
        }
        
        private sealed class MockDateResponse
        {
            [JsonPropertyName("mockDate")]
            public DateTime? MockDate { get; set; }
        }
    }
}

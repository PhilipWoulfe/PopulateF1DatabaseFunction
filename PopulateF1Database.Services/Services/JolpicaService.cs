using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Services.Services
{
    public class JolpicaService : IJolpicaService
    {
        private readonly HttpClient _httpClient;
        private readonly JolpicaApiConfig _config;

        public JolpicaService(HttpClient httpClient, IOptions<JolpicaApiConfig> config)
        {
            _httpClient = httpClient;
            _config = config.Value;
        }

        public async Task<string> GetDataAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{_config.BaseUrl}/{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}

using F1.Web.Models;
using System.Net;
using System.Net.Http.Json;

namespace F1.Web.Services.Api;

public sealed class DriversApiService(HttpClient httpClient) : IDriversApiService
{
    public async Task<Driver[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("drivers", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Driver[]>(cancellationToken) ?? [];
    }
}

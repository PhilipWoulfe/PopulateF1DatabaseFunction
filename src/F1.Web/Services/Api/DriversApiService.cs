using F1.Web.Models;
using System.Net.Http.Json;

namespace F1.Web.Services.Api;

public sealed class DriversApiService(HttpClient httpClient) : IDriversApiService
{
    public async Task<Driver[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Driver[]>("drivers", cancellationToken) ?? [];
    }
}

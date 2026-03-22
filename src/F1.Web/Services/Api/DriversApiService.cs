using System.Net;
using F1.Web.Models;

namespace F1.Web.Services.Api;

public sealed class DriversApiService(HttpClient httpClient) : IDriversApiService
{
    public async Task<Driver[]> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("drivers", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<Driver>();
        }

        return await ApiResponseParser.ReadOptionalJsonAsync(response, Array.Empty<Driver>(), "Loading drivers", cancellationToken);
    }
}

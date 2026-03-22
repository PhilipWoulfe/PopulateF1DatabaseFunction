using F1.Web.Models;
using System.Net;
using System.Net.Http.Json;

namespace F1.Web.Services.Api;

public sealed class SelectionApiService(HttpClient httpClient) : ISelectionApiService
{
    public async Task<RaceConfig> GetConfigAsync(string raceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);

        using var response = await httpClient.GetAsync($"selections/{raceId}/config", cancellationToken);
        return await ApiResponseParser.ReadRequiredJsonAsync<RaceConfig>(response, "Loading race selection config", cancellationToken);
    }

    public async Task<Selection?> GetMineAsync(string raceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);

        using var response = await httpClient.GetAsync($"selections/{raceId}/mine", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ApiResponseParser.ReadJsonOrDefaultAsync<Selection?>(response, null, "Loading my race selection", cancellationToken);
    }

    public async Task<CurrentSelectionItem[]> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("selections/current", cancellationToken);
        return await ApiResponseParser.ReadJsonOrDefaultAsync(response, Array.Empty<CurrentSelectionItem>(), "Loading current selections", cancellationToken);
    }

    public async Task<Selection> SaveMineAsync(string raceId, SelectionSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);
        ArgumentNullException.ThrowIfNull(submission);

        using var response = await httpClient.PutAsJsonAsync($"selections/{raceId}/mine", submission, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var saved = await response.Content.ReadFromJsonAsync<Selection>(cancellationToken);
            return saved ?? throw new InvalidOperationException("Selection save succeeded but the response body was empty.");
        }

        // For 400/403/404, try to extract API message via ApiResponseParser error handling
        await ApiResponseParser.EnsureSuccessAsync(response, "Saving race selection", cancellationToken);
        throw new InvalidOperationException("Unable to save selection.");
    }
}


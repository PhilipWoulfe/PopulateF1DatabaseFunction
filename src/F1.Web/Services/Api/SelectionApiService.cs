using F1.Web.Models;
using System.Net;
using System.Net.Http.Json;

namespace F1.Web.Services.Api;

public sealed class SelectionApiService(HttpClient httpClient) : ISelectionApiService
{
    public async Task<RaceConfig> GetConfigAsync(string raceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);

        var config = await httpClient.GetFromJsonAsync<RaceConfig>($"selections/{raceId}/config", cancellationToken);
        return config ?? throw new InvalidOperationException($"Selection config is missing for race '{raceId}'.");
    }

    public async Task<Selection?> GetMineAsync(string raceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);

        using var response = await httpClient.GetAsync($"selections/{raceId}/mine", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Selection>(cancellationToken);
    }

    public async Task<CurrentSelectionItem[]> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<CurrentSelectionItem[]>("selections/current", cancellationToken) ?? [];
    }

    public async Task SaveMineAsync(string raceId, SelectionSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);
        ArgumentNullException.ThrowIfNull(submission);

        using var response = await httpClient.PutAsJsonAsync($"selections/{raceId}/mine", submission, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

using System.Net;
using System.Net.Http.Json;

namespace F1.E2E.Tests.Infrastructure;

internal class ApiVerificationClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public ApiVerificationClient(E2eOptions options)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.ApiBaseUrl + "/")
        };

        var headers = options.BuildCloudflareHeaders();
        foreach (var header in headers)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    public async Task<IReadOnlyList<CurrentSelectionRow>> GetCurrentSelectionsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("selections/current", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<CurrentSelectionRow>>(cancellationToken: cancellationToken);
        return payload ?? [];
    }

    public async Task<RaceMetadataRow?> GetRaceMetadataAsync(string raceId, bool includeDraft, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"races/{raceId}/metadata?includeDraft={includeDraft.ToString().ToLowerInvariant()}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RaceMetadataRow>(cancellationToken: cancellationToken);
    }

    public async Task WaitForSelectionPersistenceAsync(string expectedDriverId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var rows = await GetCurrentSelectionsAsync(cancellationToken);
            if (rows.Any(row => string.Equals(row.DriverId, expectedDriverId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"Selection with driverId '{expectedDriverId}' was not persisted within {timeout.TotalSeconds} seconds.");
    }

    public async Task<RaceMetadataRow> WaitForMetadataAsync(string raceId, string expectedH2hQuestion, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var metadata = await GetRaceMetadataAsync(raceId, includeDraft: true, cancellationToken);
            if (metadata is not null && string.Equals(metadata.H2HQuestion, expectedH2hQuestion, StringComparison.Ordinal))
            {
                return metadata;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"Metadata update for race '{raceId}' was not observed within {timeout.TotalSeconds} seconds.");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

internal class CurrentSelectionRow
{
    public int Position { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public string SelectionType { get; set; } = string.Empty;
}

internal class RaceMetadataRow
{
    public string H2HQuestion { get; set; } = string.Empty;
    public string BonusQuestion { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

using System.Net.Http.Json;
using System.Text.Json;
using F1.DataSyncWorker.Models;

namespace F1.DataSyncWorker.Clients;

public sealed class JolpicaClient : IJolpicaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<JolpicaClient> _logger;

    public JolpicaClient(HttpClient httpClient, ILogger<JolpicaClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<IReadOnlyList<JolpicaDriverDto>> GetDriversAsync(int season, int retryCount, int retryDelayMs, CancellationToken cancellationToken)
    {
        return WithRetryAsync(
            async token =>
            {
                using var response = await _httpClient.GetAsync($"{season}/drivers.json", token);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<JolpicaDriversEnvelope>(JsonOptions, token);
                return (IReadOnlyList<JolpicaDriverDto>?)payload?.Metadata?.DriverTable?.Drivers ?? [];
            },
            retryCount,
            retryDelayMs,
            $"drivers season={season}",
            cancellationToken);
    }

    public Task<IReadOnlyList<JolpicaRaceDto>> GetRacesAsync(int season, int retryCount, int retryDelayMs, CancellationToken cancellationToken)
    {
        return WithRetryAsync(
            async token =>
            {
                using var response = await _httpClient.GetAsync($"{season}.json", token);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<JolpicaRacesEnvelope>(JsonOptions, token);
                return (IReadOnlyList<JolpicaRaceDto>?)payload?.Metadata?.RaceTable?.Races ?? [];
            },
            retryCount,
            retryDelayMs,
            $"races season={season}",
            cancellationToken);
    }

    private async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int retryCount,
        int retryDelayMs,
        string operation,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, retryCount + 1);
        var delay = TimeSpan.FromMilliseconds(Math.Max(100, retryDelayMs));

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                _logger.LogWarning(ex, "Jolpica request failed for {Operation} on attempt {Attempt}/{MaxAttempts}. Retrying.", operation, attempt, attempts);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return await action(cancellationToken);
    }
}
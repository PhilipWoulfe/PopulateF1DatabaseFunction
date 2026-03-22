using System.Net.Http.Json;
using System.Text.Json;
using F1.DataSyncWorker.Models;
using F1.DataSyncWorker.Options;
using Microsoft.Extensions.Options;

namespace F1.DataSyncWorker.Clients;

public sealed class JolpicaClient : IJolpicaClient
{
    private const string JolpicaClientName = "Jolpica";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<DataSyncOptions> _options;
    private readonly ILogger<JolpicaClient> _logger;

    public JolpicaClient(
        IHttpClientFactory httpClientFactory,
        IOptions<DataSyncOptions> options,
        ILogger<JolpicaClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public Task<IReadOnlyList<JolpicaDriverDto>> GetDriversAsync(int season, int retryCount, int retryDelayMs, CancellationToken cancellationToken)
    {
        return WithRetryAsync(
            async token =>
            {
                using var httpClient = CreateClient();
                using var response = await httpClient.GetAsync($"{season}/drivers.json", token);
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
                using var httpClient = CreateClient();
                using var response = await httpClient.GetAsync($"{season}.json", token);
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
            catch (Exception ex)
            {
                if (attempt >= attempts)
                {
                    throw;
                }

                _logger.LogWarning(ex, "Jolpica request failed for {Operation} on attempt {Attempt}/{MaxAttempts}. Retrying.", operation, attempt, attempts);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(JolpicaClientName);
        client.BaseAddress = new Uri(_options.Value.JolpicaBaseUrl, UriKind.Absolute);
        return client;
    }
}
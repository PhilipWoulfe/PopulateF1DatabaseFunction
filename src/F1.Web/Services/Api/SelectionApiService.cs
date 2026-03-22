using F1.Web.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    public async Task<Selection> SaveMineAsync(string raceId, SelectionSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);
        ArgumentNullException.ThrowIfNull(submission);

        using var response = await httpClient.PutAsJsonAsync($"selections/{raceId}/mine", submission, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var saved = await response.Content.ReadFromJsonAsync<Selection>(cancellationToken);
            return saved ?? throw new SelectionApiException(response.StatusCode, "Selection save succeeded but the response body was empty.");
        }

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            throw await CreateSelectionApiExceptionAsync(response, "Unable to save selection.", cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        throw new SelectionApiException(response.StatusCode, "Unable to save selection.");
    }

    private static async Task<SelectionApiException> CreateSelectionApiExceptionAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        try
        {
            var apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(apiError?.Message))
            {
                return new SelectionApiException(response.StatusCode, apiError.Message);
            }
        }
        catch (JsonException)
        {
            // Fall through to plain text.
        }
        catch (NotSupportedException)
        {
            // Fall through to plain text.
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(text))
        {
            return new SelectionApiException(response.StatusCode, text.Trim());
        }

        return new SelectionApiException(response.StatusCode, fallbackMessage);
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

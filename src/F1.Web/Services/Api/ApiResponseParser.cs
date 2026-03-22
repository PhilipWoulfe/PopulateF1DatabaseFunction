using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace F1.Web.Services.Api;

public static class ApiResponseParser
{
    /// <summary>
    /// Validates the HTTP status code and throws an <see cref="ApiServiceException"/> populated from the response body on failure.
    /// </summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw await CreateErrorAsync(response, operation, cancellationToken);
    }

    /// <summary>
    /// Reads a successful JSON response where a payload is required. Null, malformed JSON, and unsupported content types throw <see cref="ApiServiceException"/>.
    /// </summary>
    public static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await EnsureSuccessAsync(response, operation, cancellationToken);

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            if (payload is null)
            {
                throw new ApiServiceException(
                    new ApiError(response.StatusCode, $"{operation} returned an empty response body."));
            }

            return payload;
        }
        catch (JsonException ex)
        {
            throw new ApiServiceException(
                new ApiError(response.StatusCode, $"{operation} returned malformed JSON."),
                ex);
        }
        catch (NotSupportedException ex)
        {
            throw new ApiServiceException(
                new ApiError(response.StatusCode, $"{operation} returned an unsupported response content type."),
                ex);
        }
    }

    /// <summary>
    /// Reads a successful JSON response where a null payload is allowed and should be replaced with <paramref name="fallbackValue"/>.
    /// Non-success status codes, malformed JSON, and unsupported content types still throw <see cref="ApiServiceException"/>.
    /// </summary>
    public static async Task<T> ReadOptionalJsonAsync<T>(HttpResponseMessage response, T fallbackValue, string operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await EnsureSuccessAsync(response, operation, cancellationToken);

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return payload is null ? fallbackValue : payload;
        }
        catch (JsonException ex)
        {
            throw new ApiServiceException(
                new ApiError(response.StatusCode, $"{operation} returned malformed JSON."),
                ex);
        }
        catch (NotSupportedException ex)
        {
            throw new ApiServiceException(
                new ApiError(response.StatusCode, $"{operation} returned an unsupported response content type."),
                ex);
        }
    }

    private static async Task<ApiServiceException> CreateErrorAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        var fallbackMessage = $"{operation} failed with status code {(int)response.StatusCode}.";

        if (response.Content is null)
        {
            return new ApiServiceException(new ApiError(response.StatusCode, fallbackMessage));
        }

        try
        {
            var apiError = await response.Content.ReadFromJsonAsync<ApiErrorEnvelope>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(apiError?.Message))
            {
                return new ApiServiceException(
                    new ApiError(response.StatusCode, apiError.Message, apiError.Code));
            }
        }
        catch (JsonException)
        {
            // Fall through to plain text parsing.
        }
        catch (NotSupportedException)
        {
            // Fall through to plain text parsing.
        }

        try
        {
            var bodyText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                return new ApiServiceException(new ApiError(response.StatusCode, bodyText.Trim()));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ignore body read failures and return fallback message.
        }

        return new ApiServiceException(new ApiError(response.StatusCode, fallbackMessage));
    }

    private sealed class ApiErrorEnvelope
    {
        public string? Message { get; set; }

        public string? Code { get; set; }
    }
}

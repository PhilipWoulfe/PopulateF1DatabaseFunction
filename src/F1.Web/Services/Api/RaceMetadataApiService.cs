using F1.Web.Models;
using System.Net;

namespace F1.Web.Services.Api;

public sealed class RaceMetadataApiService(HttpClient httpClient) : IRaceMetadataApiService
{
    public async Task<RaceQuestionMetadata?> GetPublishedAsync(string raceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raceId);

        using var response = await httpClient.GetAsync($"races/{raceId}/metadata", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ApiResponseParser.ReadJsonOrDefaultAsync<RaceQuestionMetadata?>(response, null, "Loading race metadata", cancellationToken);
    }
}

using F1.Web.Models;

namespace F1.Web.Services.Api;

public interface IRaceMetadataApiService
{
    Task<RaceQuestionMetadata?> GetPublishedAsync(string raceId, CancellationToken cancellationToken = default);
}

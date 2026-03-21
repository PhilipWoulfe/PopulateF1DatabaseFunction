using F1.Core.Models;

namespace F1.Core.Interfaces;

public interface IRaceMetadataService
{
    Task<RaceQuestionMetadata?> GetMetadataAsync(string raceId, bool publishedOnly);

    Task<RaceQuestionMetadata> UpsertMetadataAsync(string raceId, RaceQuestionMetadata metadata);
}
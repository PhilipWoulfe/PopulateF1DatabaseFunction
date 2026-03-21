using F1.Core.Models;

namespace F1.Core.Interfaces;

public interface IRaceMetadataRepository
{
    Task<RaceQuestionMetadata?> GetMetadataAsync(string raceId);

    Task<RaceQuestionMetadata> UpsertMetadataAsync(string raceId, RaceQuestionMetadata metadata);
}
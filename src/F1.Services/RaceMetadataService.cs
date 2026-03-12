using F1.Core.Exceptions;
using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Services;

public class RaceMetadataService : IRaceMetadataService
{
    private readonly IRaceMetadataRepository _raceMetadataRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RaceMetadataService(IRaceMetadataRepository raceMetadataRepository, IDateTimeProvider dateTimeProvider)
    {
        _raceMetadataRepository = raceMetadataRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RaceQuestionMetadata?> GetMetadataAsync(string raceId, bool publishedOnly)
    {
        var metadata = await _raceMetadataRepository.GetMetadataAsync(raceId);
        if (metadata is null)
        {
            return null;
        }

        if (publishedOnly && !metadata.IsPublished)
        {
            return null;
        }

        return metadata;
    }

    public async Task<RaceQuestionMetadata> UpsertMetadataAsync(string raceId, RaceQuestionMetadata metadata, string? expectedEtag)
    {
        if (string.IsNullOrWhiteSpace(raceId))
        {
            throw new MetadataValidationException("Race ID is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.H2HQuestion))
        {
            throw new MetadataValidationException("H2H question is required.");
        }

        if (string.IsNullOrWhiteSpace(metadata.BonusQuestion))
        {
            throw new MetadataValidationException("Bonus question is required.");
        }

        metadata.RaceId = raceId;
        metadata.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        return await _raceMetadataRepository.UpsertMetadataAsync(raceId, metadata, expectedEtag);
    }
}
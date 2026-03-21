using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Repositories;

public class PostgresRaceMetadataRepository : IRaceMetadataRepository
{
    private readonly F1DbContext _dbContext;

    public PostgresRaceMetadataRepository(F1DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RaceQuestionMetadata?> GetMetadataAsync(string raceId)
    {
        var entity = await _dbContext.RaceMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RaceId == raceId);

        if (entity is null)
        {
            return null;
        }

        return Map(entity);
    }

    public async Task<RaceQuestionMetadata> UpsertMetadataAsync(string raceId, RaceQuestionMetadata metadata)
    {
        var raceExists = await _dbContext.Races.AnyAsync(x => x.Id == raceId);
        if (!raceExists)
        {
            throw new KeyNotFoundException($"Race document not found for raceId '{raceId}'.");
        }

        var existing = await _dbContext.RaceMetadata.FirstOrDefaultAsync(x => x.RaceId == raceId);
        if (existing is null)
        {
            existing = new RaceMetadataEntity
            {
                RaceId = raceId,
                H2HQuestion = metadata.H2HQuestion,
                BonusQuestion = metadata.BonusQuestion,
                IsPublished = metadata.IsPublished,
                UpdatedAtUtc = metadata.UpdatedAtUtc
            };
            _dbContext.RaceMetadata.Add(existing);
        }
        else
        {
            existing.H2HQuestion = metadata.H2HQuestion;
            existing.BonusQuestion = metadata.BonusQuestion;
            existing.IsPublished = metadata.IsPublished;
            existing.UpdatedAtUtc = metadata.UpdatedAtUtc;
        }

        await _dbContext.SaveChangesAsync();
        return Map(existing);
    }

    private static RaceQuestionMetadata Map(RaceMetadataEntity entity)
    {
        return new RaceQuestionMetadata
        {
            RaceId = entity.RaceId,
            H2HQuestion = entity.H2HQuestion,
            BonusQuestion = entity.BonusQuestion,
            IsPublished = entity.IsPublished,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            ETag = null
        };
    }
}

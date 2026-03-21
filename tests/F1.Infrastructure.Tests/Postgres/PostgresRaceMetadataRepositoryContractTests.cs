using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Data.Entities;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Tests.Postgres;

public class PostgresRaceMetadataRepositoryContractTests : RaceMetadataRepositoryContractTests
{
    protected override IMetadataTestRepository CreateEmptyRepository()
    {
        var context = CreateContext();
        var repository = new PostgresRaceMetadataRepository(context);
        return new MetadataTestRepository(repository);
    }

    protected override IMetadataTestRepository CreateRepositoryWithMetadata(string raceId, RaceQuestionMetadata metadata)
    {
        var context = CreateContext();
        SeedCompetitionAndRace(context, raceId);

        context.RaceMetadata.Add(new RaceMetadataEntity
        {
            RaceId = raceId,
            H2HQuestion = metadata.H2HQuestion,
            BonusQuestion = metadata.BonusQuestion,
            IsPublished = metadata.IsPublished,
            UpdatedAtUtc = metadata.UpdatedAtUtc
        });
        context.SaveChanges();

        var repository = new PostgresRaceMetadataRepository(context);
        return new MetadataTestRepository(repository);
    }

    protected override IMetadataTestRepository CreateRepositoryWithRaceDocumentOnly(string raceId)
    {
        var context = CreateContext();
        SeedCompetitionAndRace(context, raceId);

        var repository = new PostgresRaceMetadataRepository(context);
        return new MetadataTestRepository(repository);
    }

    private static F1DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<F1DbContext>()
            .UseInMemoryDatabase($"metadata-contract-{Guid.NewGuid():N}")
            .Options;

        return new F1DbContext(options);
    }

    private static void SeedCompetitionAndRace(F1DbContext context, string raceId)
    {
        context.Competitions.Add(new Competition
        {
            Id = 1,
            Name = "Main Competition",
            Year = 2025,
            Description = "Contract test competition"
        });

        context.Races.Add(new Race
        {
            Id = raceId,
            CompetitionId = 1,
            Season = 2025,
            Round = 24,
            RaceName = "Yas Marina",
            CircuitName = "Yas Marina",
            StartTimeUtc = new DateTime(2025, 12, 8, 12, 0, 0, DateTimeKind.Utc),
            PreQualyDeadlineUtc = new DateTime(2025, 12, 7, 13, 0, 0, DateTimeKind.Utc),
            FinalDeadlineUtc = new DateTime(2025, 12, 8, 12, 0, 0, DateTimeKind.Utc)
        });

        context.SaveChanges();
    }

    private sealed class MetadataTestRepository : IMetadataTestRepository
    {
        public MetadataTestRepository(PostgresRaceMetadataRepository repository)
        {
            Repository = repository;
        }

        public F1.Core.Interfaces.IRaceMetadataRepository Repository { get; }
    }
}

using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Tests.Postgres;

public class PostgresSelectionRepositoryContractTests : SelectionRepositoryContractTests
{
    protected override ISelectionRepository CreateEmptyRepository()
    {
        var context = CreateContext();
        SeedCompetitionAndRace(context, "no-such-race");
        SeedDrivers(context);
        return new PostgresSelectionRepository(context);
    }

    protected override Task<(ISelectionRepository Repo, Selection Existing)> ArrangeRepositoryWithSelection(
        string raceId, string userId, Selection selection)
    {
        var context = CreateContext();
        SeedCompetitionAndRace(context, raceId);
        SeedDrivers(context);

        var repository = new PostgresSelectionRepository(context);

        var seeded = new Selection
        {
            Id = selection.Id == Guid.Empty ? Guid.NewGuid() : selection.Id,
            RaceId = raceId,
            UserId = userId,
            BetType = selection.BetType,
            SubmittedAtUtc = selection.SubmittedAtUtc,
            OrderedSelections = selection.OrderedSelections
        };

        context.Selections.Add(new Selection
        {
            Id = seeded.Id,
            RaceId = seeded.RaceId,
            UserId = seeded.UserId,
            BetType = seeded.BetType,
            SubmittedAtUtc = seeded.SubmittedAtUtc
        });

        context.SelectionPositions.AddRange(seeded.OrderedSelections.Select(x => new F1.Infrastructure.Data.Entities.SelectionPositionEntity
        {
            SelectionId = seeded.Id,
            Position = x.Position,
            DriverId = x.DriverId
        }));

        context.SaveChanges();

        return Task.FromResult(((ISelectionRepository)repository, seeded));
    }

    private static F1DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<F1DbContext>()
            .UseInMemoryDatabase($"selection-contract-{Guid.NewGuid():N}")
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

    private static void SeedDrivers(F1DbContext context)
    {
        var driverIds = new[] { "norris", "leclerc", "hamilton", "piastri", "verstappen" };
        context.Drivers.AddRange(driverIds.Select(id => new Driver
        {
            DriverId = id,
            FullName = id
        }));
        context.SaveChanges();
    }
}

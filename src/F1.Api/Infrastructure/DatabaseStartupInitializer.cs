using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace F1.Api.Infrastructure;

public static class DatabaseStartupInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate");
        if (!autoMigrate)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<F1DbContext>();

        await dbContext.Database.MigrateAsync();
        await SeedAsync(dbContext);

        Log.Information("Database migration and seed completed.");
    }

    private static async Task SeedAsync(F1DbContext dbContext)
    {
        if (!await dbContext.Competitions.AnyAsync(x => x.Id == 1))
        {
            dbContext.Competitions.Add(new Competition
            {
                Id = 1,
                Name = "Main Competition",
                Year = 2025,
                Description = "Default seeded competition"
            });
        }

        if (!await dbContext.Races.AnyAsync(x => x.Id == SelectionService.CurrentRaceId))
        {
            dbContext.Races.Add(new Race
            {
                Id = SelectionService.CurrentRaceId,
                CompetitionId = 1,
                Season = 2025,
                Round = 24,
                RaceName = "Yas Marina Grand Prix",
                CircuitName = "Yas Marina",
                StartTimeUtc = SelectionService.FinalSubmissionDeadlineUtc,
                PreQualyDeadlineUtc = SelectionService.PreQualyDeadlineUtc,
                FinalDeadlineUtc = SelectionService.FinalSubmissionDeadlineUtc
            });
        }

        var seededDrivers = new List<Driver>
        {
            new() { DriverId = "max_verstappen", FullName = "Max Verstappen", Code = "VER", PermanentNumber = 1, Nationality = "Dutch" },
            new() { DriverId = "lando_norris", FullName = "Lando Norris", Code = "NOR", PermanentNumber = 4, Nationality = "British" },
            new() { DriverId = "charles_leclerc", FullName = "Charles Leclerc", Code = "LEC", PermanentNumber = 16, Nationality = "Monegasque" },
            new() { DriverId = "oscar_piastri", FullName = "Oscar Piastri", Code = "PIA", PermanentNumber = 81, Nationality = "Australian" },
            new() { DriverId = "lewis_hamilton", FullName = "Lewis Hamilton", Code = "HAM", PermanentNumber = 44, Nationality = "British" },
            new() { DriverId = "verstappen", FullName = "Max Verstappen", Code = "VER", PermanentNumber = 1, Nationality = "Dutch" },
            new() { DriverId = "norris", FullName = "Lando Norris", Code = "NOR", PermanentNumber = 4, Nationality = "British" },
            new() { DriverId = "leclerc", FullName = "Charles Leclerc", Code = "LEC", PermanentNumber = 16, Nationality = "Monegasque" },
            new() { DriverId = "piastri", FullName = "Oscar Piastri", Code = "PIA", PermanentNumber = 81, Nationality = "Australian" },
            new() { DriverId = "hamilton", FullName = "Lewis Hamilton", Code = "HAM", PermanentNumber = 44, Nationality = "British" }
        };

        foreach (var driver in seededDrivers)
        {
            if (!await dbContext.Drivers.AnyAsync(x => x.DriverId == driver.DriverId))
            {
                dbContext.Drivers.Add(driver);
            }
        }

        if (!await dbContext.RaceMetadata.AnyAsync(x => x.RaceId == SelectionService.CurrentRaceId))
        {
            dbContext.RaceMetadata.Add(new F1.Infrastructure.Data.Entities.RaceMetadataEntity
            {
                RaceId = SelectionService.CurrentRaceId,
                H2HQuestion = "Who finishes higher: Leclerc or Norris?",
                BonusQuestion = "How many safety-car laps will there be?",
                IsPublished = true,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }
}

using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace F1.Api.Tests;

public class SelectionServiceTests
{
    private readonly Mock<ISelectionRepository> _selectionRepositoryMock = new();
    private readonly Mock<IDriverRepository> _driverRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();

    [Fact]
    public async Task UpsertSelectionAsync_ShouldReject_WhenMoreThanFiveSelectionsSubmitted()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 7, 0, 0, 0, DateTimeKind.Utc));

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.Regular,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" },
                new SelectionPosition { Position = 0, DriverId = "" }
            }
        };

        await Assert.ThrowsAsync<SelectionValidationException>(() =>
            service.UpsertSelectionAsync("2025-24-yas_marina", "user@example.com", submission));
    }

    [Fact]
    public async Task UpsertSelectionAsync_ShouldRejectPreQualyBet_AfterDeadline()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 7, 13, 1, 0, DateTimeKind.Utc));
        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync((Selection?)null);

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.PreQualy,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        };

        await Assert.ThrowsAsync<SelectionValidationException>(() =>
            service.UpsertSelectionAsync("2025-24-yas_marina", "user@example.com", submission));
    }

    [Fact]
    public async Task UpsertSelectionAsync_ShouldAllowRegularUpdate_AfterPreQualyDeadlineBeforeFinal()
    {
        var nowUtc = new DateTime(2025, 12, 7, 14, 0, 0, DateTimeKind.Utc);
        var service = CreateServiceAt(nowUtc);

        var existing = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        };

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync(existing);

        _selectionRepositoryMock
            .Setup(repo => repo.UpsertSelectionAsync(It.IsAny<Selection>()))
            .ReturnsAsync((Selection selection) => selection);

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.Regular,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        };

        var updated = await service.UpsertSelectionAsync("2025-24-yas_marina", "user@example.com", submission);

        Assert.Equal(BetType.Regular, updated.BetType);
        Assert.Equal(nowUtc, updated.SubmittedAtUtc);
        Assert.Equal("norris", updated.OrderedSelections[0].DriverId);
    }
    [Fact]
    public void GetRaceConfig_ShouldReturnConfig_ForCurrentRace()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        var config = service.GetRaceConfig("2025-24-yas_marina");

        Assert.NotNull(config);
        Assert.Equal("2025-24-yas_marina", config.RaceId);
        Assert.Equal(new DateTime(2025, 12, 7, 13, 0, 0, DateTimeKind.Utc), config.PreQualyDeadlineUtc);
        Assert.Equal(new DateTime(2025, 12, 8, 12, 0, 0, DateTimeKind.Utc), config.FinalDeadlineUtc);
    }

    [Fact]
    public void GetRaceConfig_ShouldReturnNull_ForUnknownRace()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        var config = service.GetRaceConfig("unknown-race");

        Assert.Null(config);
    
    }
    [Fact]
    public async Task GetSelectionAsync_ShouldReturnIsLocked_AfterFinalSubmissionDeadline()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 8, 12, 1, 0, DateTimeKind.Utc));

        var existing = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        };

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync(existing);

        var result = await service.GetSelectionAsync("2025-24-yas_marina", "user@example.com");

        Assert.NotNull(result);
        Assert.True(result.IsLocked);
    }

    [Fact]
    public void CalculateScore_ShouldNotApplyPreQualyMultiplier_ForAllOrNothing()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        var score = service.CalculateScore(
            BetType.AllOrNothing,
            isPerfectTopFive: true,
            basePoints: 100,
            submittedBeforePreQualyDeadline: true);

        Assert.Equal(200, score);
    }

    [Fact]
    public async Task GetCurrentSelectionsAsync_ShouldReturnMappedRows_WhenSelectionExists()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync(new Selection
            {
                Id = Guid.NewGuid(),
                RaceId = "2025-24-yas_marina",
                UserId = "user@example.com",
                BetType = BetType.PreQualy,
                SubmittedAtUtc = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc),
                OrderedSelections = new List<SelectionPosition>
                {
                    new SelectionPosition { Position = 1, DriverId = "norris" },
                    new SelectionPosition { Position = 2, DriverId = "leclerc" }
                }
            });

        _driverRepositoryMock
            .Setup(repo => repo.GetDriversAsync())
            .ReturnsAsync([
                new Driver { DriverId = "norris", FullName = "Lando Norris" },
                new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
            ]);

        var rows = await service.GetCurrentSelectionsAsync("user@example.com");

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Position);
        Assert.Equal("user@example.com", rows[0].UserId);
        Assert.Equal("Lando Norris", rows[0].DriverName);
        Assert.Equal("PreQualy", rows[0].SelectionType);
        Assert.Equal(2, rows[1].Position);
    }

    [Fact]
    public async Task GetCurrentSelectionsAsync_ShouldReturnRowsSortedByPosition_WhenSelectionsAreOutOfOrder()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync(new Selection
            {
                Id = Guid.NewGuid(),
                RaceId = "2025-24-yas_marina",
                UserId = "user@example.com",
                BetType = BetType.Regular,
                SubmittedAtUtc = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc),
                OrderedSelections = new List<SelectionPosition>
                {
                    new SelectionPosition { Position = 3, DriverId = "hamilton" },
                    new SelectionPosition { Position = 1, DriverId = "norris" },
                    new SelectionPosition { Position = 5, DriverId = "verstappen" },
                    new SelectionPosition { Position = 2, DriverId = "leclerc" },
                    new SelectionPosition { Position = 4, DriverId = "piastri" }
                }
            });

        _driverRepositoryMock
            .Setup(repo => repo.GetDriversAsync())
            .ReturnsAsync([
                new Driver { DriverId = "norris", FullName = "Lando Norris" },
                new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
                new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
                new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
                new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
            ]);

        var rows = await service.GetCurrentSelectionsAsync("user@example.com");

        Assert.Equal(5, rows.Count);
        Assert.Equal(1, rows[0].Position);
        Assert.Equal("norris", rows[0].DriverId);
        Assert.Equal(2, rows[1].Position);
        Assert.Equal("leclerc", rows[1].DriverId);
        Assert.Equal(3, rows[2].Position);
        Assert.Equal("hamilton", rows[2].DriverId);
        Assert.Equal(4, rows[3].Position);
        Assert.Equal("piastri", rows[3].DriverId);
        Assert.Equal(5, rows[4].Position);
        Assert.Equal("verstappen", rows[4].DriverId);
    }

    [Fact]
    public async Task GetCurrentSelectionsAsync_ShouldReturnEmpty_WhenNoSelectionExists()
    {
        var service = CreateServiceAt(new DateTime(2025, 12, 6, 12, 0, 0, DateTimeKind.Utc));

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync((Selection?)null);

        var rows = await service.GetCurrentSelectionsAsync("user@example.com");

        Assert.Empty(rows);
        _driverRepositoryMock.Verify(repo => repo.GetDriversAsync(), Times.Never);
    }

     [Fact]
    public async Task UpsertSelectionAsync_ShouldAllowPreQualyBet_BeforeDeadline()
    {
        var beforeDeadline = new DateTime(2025, 12, 7, 12, 59, 0, DateTimeKind.Utc);
        var service = CreateServiceAt(beforeDeadline);

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2025-24-yas_marina", "user@example.com"))
            .ReturnsAsync((Selection?)null);

        _selectionRepositoryMock
            .Setup(repo => repo.UpsertSelectionAsync(It.IsAny<Selection>()))
            .ReturnsAsync((Selection selection) => selection);

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.PreQualy,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        };

        var result = await service.UpsertSelectionAsync("2025-24-yas_marina", "user@example.com", submission);

        Assert.NotNull(result);
        Assert.Equal(BetType.PreQualy, result.BetType);
        Assert.Equal(beforeDeadline, result.SubmittedAtUtc);
        Assert.Equal("norris", result.OrderedSelections[0].DriverId);
    }

    private SelectionService CreateServiceAt(DateTime utcNow,
        bool mockCurrentSelections = false,
        string environmentName = "Production")
    {
        
        _dateTimeProviderMock.Setup(clock => clock.UtcNow).Returns(utcNow);
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DevSettings:MockCurrentSelections", mockCurrentSelections.ToString() }
            })
            .Build();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(env => env.EnvironmentName).Returns(environmentName);

        return new SelectionService(_selectionRepositoryMock.Object, _driverRepositoryMock.Object, _dateTimeProviderMock.Object, configuration, hostEnvironment.Object);
    }
}

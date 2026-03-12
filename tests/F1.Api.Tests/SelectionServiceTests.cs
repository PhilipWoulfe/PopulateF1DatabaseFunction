using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Services;
using Moq;

namespace F1.Api.Tests;

public class SelectionServiceTests
{
    private readonly Mock<ISelectionRepository> _selectionRepositoryMock = new();
    private readonly Mock<IDriverRepository> _driverRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();

    [Fact]
    public async Task UpsertSelectionAsync_ShouldRejectPreQualyBet_AfterDeadline()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 7, 4, 31, 0, DateTimeKind.Utc));
        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2026-australia", "user@example.com"))
            .ReturnsAsync((Selection?)null);

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.PreQualy,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        await Assert.ThrowsAsync<SelectionValidationException>(() =>
            service.UpsertSelectionAsync("2026-australia", "user@example.com", submission));
    }

    [Fact]
    public async Task UpsertSelectionAsync_ShouldAllowRegularUpdate_AfterPreQualyDeadlineBeforeFinal()
    {
        var nowUtc = new DateTime(2026, 3, 7, 10, 0, 0, DateTimeKind.Utc);
        var service = CreateServiceAt(nowUtc);

        var existing = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2026-australia",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2026-australia", "user@example.com"))
            .ReturnsAsync(existing);

        _selectionRepositoryMock
            .Setup(repo => repo.UpsertSelectionAsync(It.IsAny<Selection>()))
            .ReturnsAsync((Selection selection) => selection);

        var submission = new SelectionSubmissionDto
        {
            BetType = BetType.Regular,
            Selections = ["leclerc", "norris", "hamilton", "piastri", "verstappen"]
        };

        var updated = await service.UpsertSelectionAsync("2026-australia", "user@example.com", submission);

        Assert.Equal(BetType.Regular, updated.BetType);
        Assert.Equal(nowUtc, updated.SubmittedAtUtc);
        Assert.Equal("leclerc", updated.Selections[0]);
    }
    [Fact]
    public void GetRaceConfig_ShouldReturnConfig_ForAustraliaRace()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc));

        var config = service.GetRaceConfig("2026-australia");

        Assert.NotNull(config);
        Assert.Equal("2026-australia", config.RaceId);
        Assert.Equal(new DateTime(2026, 3, 7, 4, 30, 0, DateTimeKind.Utc), config.PreQualyDeadlineUtc);
        Assert.Equal(new DateTime(2026, 3, 8, 3, 30, 0, DateTimeKind.Utc), config.FinalDeadlineUtc);
    }

    [Fact]
    public void GetRaceConfig_ShouldReturnNull_ForUnknownRace()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc));

        var config = service.GetRaceConfig("unknown-race");

        Assert.Null(config);
    
    }
    [Fact]
    public async Task GetSelectionAsync_ShouldReturnIsLocked_AfterFinalSubmissionDeadline()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 8, 3, 31, 0, DateTimeKind.Utc));

        var existing = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2026-australia",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2026-australia", "user@example.com"))
            .ReturnsAsync(existing);

        var result = await service.GetSelectionAsync("2026-australia", "user@example.com");

        Assert.NotNull(result);
        Assert.True(result.IsLocked);
    }

    [Fact]
    public void CalculateScore_ShouldNotApplyPreQualyMultiplier_ForAllOrNothing()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc));

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
        var service = CreateServiceAt(new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc));

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2026-australia", "user@example.com"))
            .ReturnsAsync(new Selection
            {
                Id = Guid.NewGuid(),
                RaceId = "2026-australia",
                UserId = "user@example.com",
                BetType = BetType.PreQualy,
                SubmittedAtUtc = new DateTime(2026, 3, 6, 10, 0, 0, DateTimeKind.Utc),
                Selections = ["norris", "leclerc"]
            });

        _driverRepositoryMock
            .Setup(repo => repo.GetDriversAsync())
            .ReturnsAsync([
                new Driver { DriverId = "norris", FullName = "Lando Norris" },
                new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
            ]);

        var rows = await service.GetCurrentSelectionsAsync("user@example.com");

        Assert.Equal(2, rows.Count);
        Assert.Equal("user@example.com", rows[0].UserId);
        Assert.Equal("Lando Norris", rows[0].DriverName);
        Assert.Equal("PreQualy", rows[0].SelectionType);
    }

    [Fact]
    public async Task GetCurrentSelectionsAsync_ShouldReturnEmpty_WhenNoSelectionExists()
    {
        var service = CreateServiceAt(new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc));

        _selectionRepositoryMock
            .Setup(repo => repo.GetSelectionAsync("2026-australia", "user@example.com"))
            .ReturnsAsync((Selection?)null);

        var rows = await service.GetCurrentSelectionsAsync("user@example.com");

        Assert.Empty(rows);
        _driverRepositoryMock.Verify(repo => repo.GetDriversAsync(), Times.Never);
    }

    private SelectionService CreateServiceAt(DateTime utcNow)
    {
        _dateTimeProviderMock.Setup(clock => clock.UtcNow).Returns(utcNow);
        return new SelectionService(_selectionRepositoryMock.Object, _driverRepositoryMock.Object, _dateTimeProviderMock.Object);
    }
}

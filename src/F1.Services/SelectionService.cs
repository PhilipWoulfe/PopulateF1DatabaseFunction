using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Services;

public class SelectionService : ISelectionService
{
    public static readonly DateTime PreQualyDeadlineUtc = new(2026, 3, 7, 4, 30, 0, DateTimeKind.Utc);
    public static readonly DateTime FinalSubmissionDeadlineUtc = new(2026, 3, 8, 3, 30, 0, DateTimeKind.Utc);
    public const string AustraliaRaceId2026 = "2026-australia";

    private readonly ISelectionRepository _selectionRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SelectionService(
        ISelectionRepository selectionRepository,
        IDriverRepository driverRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _selectionRepository = selectionRepository;
        _driverRepository = driverRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Selection?> GetSelectionAsync(string raceId, string userId)
    {
        var selection = await _selectionRepository.GetSelectionAsync(raceId, userId);
        if (selection is null)
        {
            return null;
        }

        selection.OrderedSelections = selection.OrderedSelections;

        var nowUtc = _dateTimeProvider.UtcNow;
        selection.IsLocked = IsPreQualyLocked(selection, nowUtc) || nowUtc > FinalSubmissionDeadlineUtc;
        return selection;
    }

    public async Task<Selection> UpsertSelectionAsync(string raceId, string userId, SelectionSubmissionDto submission)
    {
        var orderedSelections = submission.OrderedSelections;
        ValidateSelections(orderedSelections);

        var nowUtc = _dateTimeProvider.UtcNow;
        var existingSelection = await _selectionRepository.GetSelectionAsync(raceId, userId);

        if (existingSelection is not null && IsPreQualyLocked(existingSelection, nowUtc))
        {
            throw new SelectionForbiddenException("Pre-Qualy locked selections cannot be edited after Saturday 04:30 UTC.");
        }

        if (submission.BetType == BetType.PreQualy && nowUtc > PreQualyDeadlineUtc)
        {
            throw new SelectionValidationException("Pre-Qualy strategy is no longer available after Saturday 04:30 UTC.");
        }

        if (nowUtc > FinalSubmissionDeadlineUtc)
        {
            throw new SelectionForbiddenException("Selections are locked after Sunday 03:30 UTC.");
        }

        var selection = existingSelection ?? new Selection();
        selection.Id = existingSelection?.Id ?? Guid.Empty;
        selection.RaceId = raceId;
        selection.UserId = userId;
        selection.OrderedSelections = orderedSelections;
        selection.BetType = submission.BetType;
        selection.SubmittedAtUtc = nowUtc;
        selection.IsLocked = false;

        return await _selectionRepository.UpsertSelectionAsync(selection);
    }

    public async Task<IReadOnlyList<CurrentSelectionDto>> GetCurrentSelectionsAsync(string userId)
    {
        var selection = await _selectionRepository.GetSelectionAsync(AustraliaRaceId2026, userId);
        if (selection is null)
        {
            return [];
        }

        var orderedSelections = selection.OrderedSelections;

        var drivers = await _driverRepository.GetDriversAsync();
        var driverLookup = drivers
            .Where(driver => !string.IsNullOrWhiteSpace(driver.DriverId))
            .ToDictionary(driver => driver.DriverId!, driver => driver.FullName ?? driver.DriverId!, StringComparer.OrdinalIgnoreCase);

        var rows = new List<CurrentSelectionDto>(orderedSelections.Count);
        foreach (var selectionItem in orderedSelections)
        {
            if (string.IsNullOrWhiteSpace(selectionItem.DriverId))
            {
                continue;
            }

            rows.Add(new CurrentSelectionDto
            {
                Position = selectionItem.Position,
                UserId = selection.UserId,
                UserName = selection.UserId,
                DriverId = selectionItem.DriverId,
                DriverName = driverLookup.GetValueOrDefault(selectionItem.DriverId, selectionItem.DriverId),
                SelectionType = selection.BetType.ToString(),
                Timestamp = selection.SubmittedAtUtc
            });
        }

        return rows;
    }

    public RaceConfigDto? GetRaceConfig(string raceId)
    {
        if (raceId == AustraliaRaceId2026)
        {
            return new RaceConfigDto
            {
                RaceId = AustraliaRaceId2026,
                PreQualyDeadlineUtc = PreQualyDeadlineUtc,
                FinalDeadlineUtc = FinalSubmissionDeadlineUtc
            };
        }

        return null;
    }

    public int CalculateScore(BetType betType, bool isPerfectTopFive, int basePoints, bool submittedBeforePreQualyDeadline)
    {
        if (betType == BetType.AllOrNothing)
        {
            return isPerfectTopFive ? 200 : 0;
        }

        if (betType == BetType.PreQualy && submittedBeforePreQualyDeadline)
        {
            return (int)Math.Round(basePoints * 1.5m, MidpointRounding.AwayFromZero);
        }

        return basePoints;
    }

    private static void ValidateSelections(List<SelectionPosition> selections)
    {
        var validSelections = selections
            .Where(item => !string.IsNullOrWhiteSpace(item.DriverId))
            .ToList();

        var distinctPositions = validSelections
            .Select(item => item.Position)
            .Distinct()
            .Count();

        var distinctCount = selections
            .Where(item => !string.IsNullOrWhiteSpace(item.DriverId))
            .Select(item => item.DriverId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (validSelections.Count != 5 || distinctCount != 5 || distinctPositions != 5)
        {
            throw new SelectionValidationException("Exactly 5 unique drivers must be selected.");
        }

        if (validSelections.Any(item => item.Position < 1 || item.Position > 5))
        {
            throw new SelectionValidationException("Selection positions must be between 1 and 5.");
        }
    }

    private static List<SelectionPosition> NormalizeOrderedSelections(
        List<SelectionPosition>? orderedSelections,
        List<string>? legacySelections)
    {
        var normalized = (orderedSelections ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.DriverId))
            .Select(item => new SelectionPosition
            {
                Position = item.Position,
                DriverId = item.DriverId
            })
            .OrderBy(item => item.Position)
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        var fromLegacy = (legacySelections ?? [])
            .Where(driverId => !string.IsNullOrWhiteSpace(driverId))
            .Select((driverId, index) => new SelectionPosition
            {
                Position = index + 1,
                DriverId = driverId
            })
            .ToList();

        return fromLegacy;
    }

    private static bool IsPreQualyLocked(Selection selection, DateTime nowUtc)
    {
        return selection.BetType == BetType.PreQualy && nowUtc > PreQualyDeadlineUtc;
    }
}

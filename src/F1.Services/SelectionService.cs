using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Services;

public class SelectionService : ISelectionService
{
    public const string AustraliaRaceId2026 = "2026-australia";
    internal static readonly DateTime PreQualyDeadlineUtc = new(2026, 3, 7, 4, 30, 0, DateTimeKind.Utc);
    internal static readonly DateTime FinalSubmissionDeadlineUtc = new(2026, 3, 8, 3, 30, 0, DateTimeKind.Utc);

    private readonly ISelectionRepository _selectionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SelectionService(ISelectionRepository selectionRepository, IDateTimeProvider dateTimeProvider)
    {
        _selectionRepository = selectionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Selection?> GetSelectionAsync(string raceId, string userId)
    {
        var selection = await _selectionRepository.GetSelectionAsync(raceId, userId);
        if (selection is null)
        {
            return null;
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        selection.IsLocked = IsPreQualyLocked(selection, nowUtc) || nowUtc > FinalSubmissionDeadlineUtc;
        return selection;
    }

    public async Task<Selection> UpsertSelectionAsync(string raceId, string userId, SelectionSubmissionDto submission)
    {
        ValidateSelections(submission.Selections);

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
        selection.Selections = submission.Selections;
        selection.BetType = submission.BetType;
        selection.SubmittedAtUtc = nowUtc;
        selection.IsLocked = false;

        return await _selectionRepository.UpsertSelectionAsync(selection);
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

    private static void ValidateSelections(List<string> selections)
    {
        var distinctCount = selections
            .Where(driverId => !string.IsNullOrWhiteSpace(driverId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (selections.Count != 5 || distinctCount != 5)
        {
            throw new SelectionValidationException("Exactly 5 unique drivers must be selected.");
        }
    }

    private static bool IsPreQualyLocked(Selection selection, DateTime nowUtc)
    {
        return selection.BetType == BetType.PreQualy && nowUtc > PreQualyDeadlineUtc;
    }
}

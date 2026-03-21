using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Repositories;

public class PostgresSelectionRepository : ISelectionRepository
{
    private readonly F1DbContext _dbContext;

    public PostgresSelectionRepository(F1DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Selection?> GetSelectionAsync(string raceId, string userId)
    {
        var selection = await _dbContext.Selections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RaceId == raceId && x.UserId == userId);

        if (selection is null)
        {
            return null;
        }

        var orderedSelections = await _dbContext.SelectionPositions
            .AsNoTracking()
            .Where(x => x.SelectionId == selection.Id)
            .OrderBy(x => x.Position)
            .Select(x => new SelectionPosition
            {
                Position = x.Position,
                DriverId = x.DriverId
            })
            .ToListAsync();

        selection.OrderedSelections = orderedSelections;
        return selection;
    }

    public async Task<Selection> UpsertSelectionAsync(Selection selection)
    {
        var existing = await _dbContext.Selections
            .FirstOrDefaultAsync(x => x.RaceId == selection.RaceId && x.UserId == selection.UserId);

        var selectionId = existing?.Id ?? (selection.Id == Guid.Empty ? Guid.NewGuid() : selection.Id);

        if (existing is null)
        {
            _dbContext.Selections.Add(new Selection
            {
                Id = selectionId,
                RaceId = selection.RaceId,
                UserId = selection.UserId,
                BetType = selection.BetType,
                SubmittedAtUtc = selection.SubmittedAtUtc
            });
        }
        else
        {
            existing.BetType = selection.BetType;
            existing.SubmittedAtUtc = selection.SubmittedAtUtc;
        }

        var existingPositions = _dbContext.SelectionPositions.Where(x => x.SelectionId == selectionId);
        _dbContext.SelectionPositions.RemoveRange(existingPositions);

        var newPositions = selection.OrderedSelections
            .OrderBy(x => x.Position)
            .Select(x => new SelectionPositionEntity
            {
                SelectionId = selectionId,
                Position = x.Position,
                DriverId = x.DriverId
            });

        await _dbContext.SelectionPositions.AddRangeAsync(newPositions);
        await _dbContext.SaveChangesAsync();

        var updated = await GetSelectionAsync(selection.RaceId, selection.UserId);
        return updated ?? throw new InvalidOperationException("Failed to read selection after upsert.");
    }
}

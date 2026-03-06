using F1.Core.Models;

namespace F1.Core.Interfaces;

public interface ISelectionRepository
{
    Task<Selection?> GetSelectionAsync(string raceId, string userId);
    Task<Selection> UpsertSelectionAsync(Selection selection);
}

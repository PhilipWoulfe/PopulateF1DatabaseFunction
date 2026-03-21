using F1.Core.Models;

namespace F1.Core.Interfaces;

public interface IRaceRepository
{
    Task<Race?> GetRaceAsync(string raceId);
    Task<IReadOnlyList<Race>> GetRacesAsync();
}

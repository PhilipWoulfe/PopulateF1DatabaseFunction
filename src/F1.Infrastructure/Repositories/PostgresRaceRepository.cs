using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Repositories;

public class PostgresRaceRepository : IRaceRepository
{
    private readonly F1DbContext _dbContext;

    public PostgresRaceRepository(F1DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Race?> GetRaceAsync(string raceId)
    {
        return await _dbContext.Races
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == raceId);
    }

    public async Task<IReadOnlyList<Race>> GetRacesAsync()
    {
        return await _dbContext.Races
            .AsNoTracking()
            .OrderBy(x => x.StartTimeUtc)
            .ToListAsync();
    }
}

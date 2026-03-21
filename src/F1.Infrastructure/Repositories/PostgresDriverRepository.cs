using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Repositories;

public class PostgresDriverRepository : IDriverRepository
{
    private readonly F1DbContext _dbContext;

    public PostgresDriverRepository(F1DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Driver>> GetDriversAsync()
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .ToListAsync();
    }
}

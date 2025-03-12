using PopulateF1Database.DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Repositories
{
    public class RaceRepository(ICosmoDataRepository cosmosDataRepository, ILogger<RaceRepository> logger) : IRaceRepository
    {
        public async Task WriteRacesAsync(RaceListResponse raceListResponse)
        {
            try
            {
                await cosmosDataRepository.UpsertItemsAsync(raceListResponse.Races);
            }
            catch (AggregateException ex)
            {
                foreach (var error in ex.InnerExceptions)
                {
                    logger.LogError(error, "An error occurred while writing races.");
                }
                throw new AggregateException("One or more errors occurred while writing races.", ex.InnerExceptions);
            }
        }
    }
}
using JolpicaApi.Responses.RaceInfo;
using Microsoft.Extensions.Logging;
using PopulateF1Database.DataAccess.Interfaces;

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
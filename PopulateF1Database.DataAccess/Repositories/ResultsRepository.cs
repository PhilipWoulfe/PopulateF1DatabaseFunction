using PopulateF1Database.DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Repositories
{
    public class ResultsRepository(ICosmoDataRepository cosmosDataRepository, ILogger<ResultsRepository> logger) : IResultsRepository
    {
        public async Task WriteResultsAsync(RaceResultsResponse raceResultsResponse)
        {
            try
            {
                await cosmosDataRepository.UpsertItemsAsync(raceResultsResponse.Races);
            }
            catch (AggregateException ex)
            {
                foreach (var error in ex.InnerExceptions)
                {
                    logger.LogError(error, "An error occurred while writing results.");
                }
                throw new AggregateException("One or more errors occurred while writing results.", ex.InnerExceptions);
            }
        }
    }
}
using Microsoft.Extensions.Logging;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;

namespace PopulateF1Database.DataAccess.Repositories
{
    public class ResultsRepository(ICosmoDataRepository cosmosDataRepository, ILogger<ResultsRepository> logger) : IResultsRepository
    {
        public async Task WriteResultsAsync(RaceResultsResponse raceResultsResponse)
        {
            try
            {
                if (raceResultsResponse is null)
                {
                    logger.LogError("raceResultsResponse is null in WriteResultsAsync.");
                    throw new ArgumentNullException(nameof(raceResultsResponse));
                }

                if (raceResultsResponse.Races is null)
                {
                    logger.LogError("raceResultsResponse.Races is null in WriteResultsAsync.");
                    throw new InvalidOperationException("Race results response contains a null Races collection.");
                }

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

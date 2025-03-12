using PopulateF1Database.DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using PopulateF1Database.Models;
using Microsoft.Azure.Cosmos;

namespace PopulateF1Database.DataAccess.Repositories
{
    public class DriverRepository(ICosmoDataRepository cosmosDataRepository, 
        ILogger<DriverRepository> logger) : IDriverRepository
    {
        public async Task WriteDriversAsync(DriverResponse driverResponse)
        {
            try
            {
                await cosmosDataRepository.UpsertItemsAsync(driverResponse.Drivers);
            }
            catch (AggregateException ex)
            {
                foreach (var error in ex.InnerExceptions)
                {
                    logger.LogError(error, "An error occurred while writing drivers.");
                }
                throw new AggregateException("One or more errors occurred while writing drivers.", ex.InnerExceptions);
            }
        }

        //public async Task<List<PreSeasonQuestion>> ReadPreSeasonQuestionsAsync()
        //{
        //    try
        //    {
        //        var sqlQueryText = "SELECT * FROM c";
        //        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        //        FeedIterator<PreSeasonQuestion> queryResultSetIterator = cosmosDataRepository.GetItemQueryIterator<PreSeasonQuestion>(queryDefinition, "PreSeasonQuestions");

        //        List<PreSeasonQuestion> questions = new List<PreSeasonQuestion>();

        //        while (queryResultSetIterator.HasMoreResults)
        //        {
        //            FeedResponse<PreSeasonQuestion> currentResultSet = await queryResultSetIterator.ReadNextAsync();
        //            questions.AddRange(currentResultSet);
        //        }

        //        return questions;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "An error occurred while reading PreSeasonQuestions.");
        //        throw;
        //    }
        //}
    }
}
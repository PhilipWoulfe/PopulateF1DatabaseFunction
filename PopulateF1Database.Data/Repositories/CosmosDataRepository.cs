using Microsoft.Azure.Cosmos;
using PopulateF1Database.Data.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PopulateF1Database.Data.Repositories
{
    public class CosmosDataRepository : IDataRepository
    {
        private readonly Container _container;

        public CosmosDataRepository(string cosmosDBConnectionString, string cosmosDBDatabaseId, string cosmosDBContainerId)
        {
            var cosmosClient = new CosmosClient(cosmosDBConnectionString);
            _container = cosmosClient.GetContainer(cosmosDBDatabaseId, cosmosDBContainerId);
        }

        public async Task<List<dynamic>> GetItemsAsync()
        {
            var sqlQueryText = "SELECT * FROM c";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<dynamic> queryResultSetIterator = _container.GetItemQueryIterator<dynamic>(queryDefinition);

            List<dynamic> items = new List<dynamic>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                items.AddRange(currentResultSet);
            }

            return items;
        }
    }
}
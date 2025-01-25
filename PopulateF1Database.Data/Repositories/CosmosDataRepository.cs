using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
using PopulateF1Database.Data.Interfaces;

namespace PopulateF1Database.Data.Repositories
{
    public class CosmosDataRepository : IDataRepository
    {
        private readonly Container _container;

        public CosmosDataRepository(IOptions<AppConfig> config)
        {
            var cosmosClient = new CosmosClient(config.Value.CosmosDBConnectionString);
            _container = cosmosClient.GetContainer(config.Value.CosmosDBDatabaseId, config.Value.CosmosDBContainerId);
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
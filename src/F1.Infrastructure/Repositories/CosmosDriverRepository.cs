using F1.Core.Interfaces;
using F1.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace F1.Infrastructure.Repositories
{
    public class CosmosDriverRepository : IDriverRepository
    {
        private readonly Container _container;

        public CosmosDriverRepository(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var containerName = "Drivers";
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<List<Driver>> GetDriversAsync()
        {
            var query = _container.GetItemQueryIterator<Driver>(new QueryDefinition("SELECT * FROM c"));
            var results = new List<Driver>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }
    }
}

using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using PopulateF1Database.Config;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.Models;


namespace PopulateF1Database.DataAccess.Repositories
{
    public class CosmosDataRepository : ICosmoDataRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Dictionary<Type, Container> _containers;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly CosmoDbConfig _config;

        public CosmosDataRepository(CosmoDbConfig config)
        {
            _cosmosClient = new CosmosClient(config.CosmosDbConnectionString);
            _containers = new Dictionary<Type, Container>
            {
                { typeof(Driver), _cosmosClient.GetContainer(config.CosmosDbDatabaseId, config.Containers.DriversContainer) },
                { typeof(RaceWithResults), _cosmosClient.GetContainer(config.CosmosDbDatabaseId, config.Containers.ResultsContainer) }
            };

            _retryPolicy = Policy.Handle<Exception>()
                                 .WaitAndRetryAsync(config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(config.RetryTime, retryAttempt)),
                                 (exception, timeSpan, retryCount, context) =>
                                 {
                                     // Log retry attempt here if needed
                                     Console.WriteLine($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                                 });

            _config = config;
        }

        public async Task UpsertItemAsync<T>(T item) where T : class
        {
            await UpsertAsync(item);
        }

        public async Task UpsertItemsAsync<T>(IEnumerable<T> items) where T : class
        {
            var tasks = new List<Task>();
            foreach (var item in items)
            {
                tasks.Add(UpsertAsync(item));
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (AggregateException ex)
            {
                var errors = ex.InnerExceptions;
                throw new AggregateException("One or more errors occurred while writing items.", errors);
            }
        }

        private async Task UpsertAsync<T>(T item) where T : class
        {
            if (_containers.TryGetValue(typeof(T), out var container))
            {
                try
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        await container.UpsertItemAsync(item);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error upserting item of type {typeof(T).Name}: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"No container configured for type {typeof(T).Name}");
                throw new InvalidOperationException($"No container configured for type {typeof(T)}, {_containers[_containers.Keys.LastOrDefault()]}");
            }
        }
    }
}
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using PopulateF1Database.Config;
using PopulateF1Database.DataAccess.Interfaces;
//using JolpicaApi.Responses.Models;
//using JolpicaApi.Responses.Models.RaceInfo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PopulateF1Database.Models;
using Newtonsoft.Json;

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
                { typeof(Race), _cosmosClient.GetContainer(config.CosmosDbDatabaseId, config.Containers.RacesContainer) },
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
            SetIdIfNotExists(item);
            await UpsertAsync(item);
        }

        public async Task UpsertItemsAsync<T>(IEnumerable<T> items) where T : class
        {
            var tasks = new List<Task>();
            foreach (var item in items)
            {
                SetIdIfNotExists(item);
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

        //public FeedIterator<T> GetItemQueryIterator<T>(QueryDefinition queryDefinition, string containerId) where T : class
        //{
        //    var container = _cosmosClient.GetContainer(_config.CosmosDbDatabaseId, containerId);
        //    return container.GetItemQueryIterator<T>(queryDefinition);
        //}

        private async Task UpsertAsync<T>(T item) where T : class
        {
            if (_containers.TryGetValue(typeof(T), out var container))
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await container.UpsertItemAsync(item);
                });
            }
            else
            {
                throw new InvalidOperationException($"No container configured for type {typeof(T).Name}");
            }
        }

        private void SetIdIfNotExists<T>(T item) where T : class
        {
            var propertyInfo = typeof(T).GetProperty("Id");
            if (propertyInfo != null && propertyInfo.GetValue(item) == null)
            {
                propertyInfo.SetValue(item, Guid.NewGuid().ToString());
            }
        }

        private string GetPartitionKey<T>(T item) where T : class
        {
            // Implement logic to extract the partition key from the item
            // For example, if the partition key is based on a property called "Id":
            var propertyInfo = typeof(T).GetProperty("Id");
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(item)?.ToString();
            }
            throw new InvalidOperationException("Partition key property not found.");
        }

        //public async Task WritePreSeasonQuestionsAsync()
        //{
        //    var container = _cosmosClient.GetContainer(_config.CosmosDbDatabaseId, "PreSeasonQuestions");

        //    var sqlQueryText = "SELECT * FROM c";
        //    QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            
        //    FeedIterator<PreSeasonQuestion> queryResultSetIterator = container.GetItemQueryIterator<PreSeasonQuestion>(queryDefinition);

        //    List<PreSeasonQuestion> questions = new List<PreSeasonQuestion>();

        //    while (queryResultSetIterator.HasMoreResults)
        //    {
        //        FeedResponse<PreSeasonQuestion> currentResultSet = await queryResultSetIterator.ReadNextAsync();
        //        questions.AddRange(currentResultSet);
        //    }

        //    questions[0].Id = Guid.NewGuid().ToString();
        //    questions[0].QuestionId = 77;
        //    await container.CreateItemAsync(questions[0]);
        //}
    }
    //public class PreSeasonQuestion
    //{
    //    [JsonProperty(PropertyName = "questionId")]
    //    public int QuestionId { get; set; }

    //    [JsonProperty(PropertyName = "questionText")]
    //    public string QuestionText { get; set; }

    //    [JsonProperty(PropertyName = "questionAnswer")]
    //    public string QuestionAnswer { get; set; }

    //    [JsonProperty(PropertyName = "id")]
    //    public string Id { get; set; }

    //    [JsonProperty(PropertyName = "_rid")]
    //    public string _rid { get; set; }

    //    [JsonProperty(PropertyName = "_self")]
    //    public string _self { get; set; }

    //    [JsonProperty(PropertyName = "_etag")]
    //    public string _etag { get; set; }

    //    [JsonProperty(PropertyName = "_attachments")]
    //    public string _attachments { get; set; }

    //    [JsonProperty(PropertyName = "_ts")]
    //    public int _ts { get; set; }
    //}
}
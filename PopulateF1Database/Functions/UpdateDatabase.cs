//using System;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using JolpicaApi.Client;
//using JolpicaApi.Ids;
//using JolpicaApi.Requests;
//using JolpicaApi.Requests.Standard;
//using JolpicaApi.Responses.RaceInfo;
//using Microsoft.Extensions.Options;
//using PopulateF1Database.Config;

//namespace PopulateF1Database.Functions
//{
//    public class UpdateDatabase
//    {
//        private readonly ILogger _logger;
//        private readonly IJolpicaClient _jolpicaClient;

//        public UpdateDatabase(ILoggerFactory loggerFactory, IJolpicaClient jolpicaClient)
//        {
//            _logger = loggerFactory.CreateLogger<UpdateDatabase>();
//            _jolpicaClient = jolpicaClient;
//        }

//        [Function("Function1")]
//        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
//        {
//            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

//            if (myTimer.ScheduleStatus is not null)
//            {
//                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
//            }

//            // The client should be stored and reused during the lifetime of your application
//            //var client = new JolpicaClient();
//            //client.ApiBase = "https://api.jolpi.ca/ergast/f1/";

//            // All request properties are optional (except 'Season' if 'Round' is set)
//            var request = new RaceResultsRequest
//            {
//                Season = "2017",     // or Seasons.Current for current season
//                Round = "11",        // or Rounds.Last or Rounds.Next for last or next round
//                DriverId = "vettel", // or Drivers.SebastianVettel

//                Limit = 30,      // Limit the number of results returned
//                Offset = 0      // Result offset (used for paging)
//            };

//            // RaceResultsRequest returns a RaceResultsResponse
//            // Other requests returns other response types
//            RaceResultsResponse response = await _jolpicaClient.GetResponseAsync(request);
//        }
//    }
//}
using JolpicaApi.Client;
using JolpicaApi.Requests.Standard;
using JolpicaApi.Responses.RaceInfo;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
//using PopulateF1Database.DataAccess.Interfaces;
//using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Functions
{
    public class UpdateDatabase(
        ILoggerFactory loggerFactory,
        IJolpicaClient jolpicaClient
        //IOptions<AppConfig> config)
    //IDataRepository dataRepository,
    //IJolpicaService jolpicaService
    )
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateDatabase>();
        private readonly IJolpicaClient _jolpicaClient = jolpicaClient;
        //private readonly AppConfig _config = config.Value;
        //private readonly IDataRepository _dataRepository = dataRepository;
        //private readonly IJolpicaService _jolpicaService = jolpicaService;

        [Function("UpdateDatabase")]
        public async Task Run([TimerTrigger("%UpdateDatabaseCronSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
            }

            // Use the configuration values as needed
            //_logger.LogInformation("Cosmos DB Connection String: {connectionString}", _config.CosmoDb.CosmosDbConnectionString);

            // Fetch data from Jolpica API
            // All request properties are optional (except 'Season' if 'Round' is set)
            var request = new RaceResultsRequest
            {
                Season = "2017",     // or Seasons.Current for current season
                Round = "11",        // or Rounds.Last or Rounds.Next for last or next round
                DriverId = "vettel", // or Drivers.SebastianVettel

                Limit = 30,      // Limit the number of results returned
                Offset = 0      // Result offset (used for paging)
            };

            // RaceResultsRequest returns a RaceResultsResponse
            // Other requests returns other response types
            RaceResultsResponse response = await _jolpicaClient.GetResponseAsync(request);

            //var items = await _dataRepository.GetItemsAsync();
            //// Log the retrieved items
            //_logger.LogInformation($"Retrieved {items.Count} items from Cosmos DB.");
        }
    }
}
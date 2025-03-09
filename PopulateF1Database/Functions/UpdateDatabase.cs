using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
using PopulateF1Database.Data.Interfaces;
using PopulateF1Database.Services.Interfaces;

namespace PopulateF1Database.Functions
{
    public class UpdateDatabase(
        ILoggerFactory loggerFactory, 
        IOptions<AppConfig> config, 
        IDataRepository dataRepository, 
        IJolpicaService jolpicaService)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateDatabase>();
        private readonly AppConfig _config = config.Value;
        private readonly IDataRepository _dataRepository = dataRepository;
        private readonly IJolpicaService _jolpicaService = jolpicaService;

        [Function("UpdateDatabase")]
        public async Task Run([TimerTrigger("%UpdateDatabaseCronSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
            }

            // Use the configuration values as needed
            _logger.LogInformation("Jolpica API Base URL: {baseUrl}", _config.JolpicaApi.BaseUrl);
            _logger.LogInformation("Cosmos DB Connection String: {connectionString}", _config.CosmoDb.CosmosDbConnectionString);

            // Fetch data from Jolpica API
            var apiData = await _jolpicaService.GetDataAsync();

            //var items = await _dataRepository.GetItemsAsync();
            //// Log the retrieved items
            //_logger.LogInformation($"Retrieved {items.Count} items from Cosmos DB.");
        }
    }
}
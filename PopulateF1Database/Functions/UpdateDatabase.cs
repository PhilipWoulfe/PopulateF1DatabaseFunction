using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;

namespace PopulateF1Database.Functions
{
    public class UpdateDatabase
    {
        private readonly ILogger _logger;
        private readonly AppConfig _config;

        public UpdateDatabase(ILoggerFactory loggerFactory, IOptions<AppConfig> config)
        {
            _logger = loggerFactory.CreateLogger<UpdateDatabase>();
            _config = config.Value;
        }

        [Function("UpdateDatabase")]
        public void Run([TimerTrigger("%UpdateDatabaseCronSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
            }

            // Use the configuration values as needed
            _logger.LogInformation("Jolpica API Base URL: {baseUrl}", _config.JolpicaApi.BaseUrl);
            _logger.LogInformation("Cosmos DB Connection String: {connectionString}", _config.CosmosDBConnectionString);
        }
    }
}
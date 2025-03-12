using JolpicaApi.Client;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PopulateF1Database.Config;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.DataAccess.Repositories;
using PopulateF1Database.Services.Drivers.Mappers;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Services;
using PopulateF1Database.Services.Drivers.CommandHandlers;
using PopulateF1Database.Services.Results.CommandHandlers;
using PopulateF1Database.Services.Rounds.CommandHandlers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var services = builder.Services;

// Register AppConfig as a singleton
AppConfig appConfig = GetAppConfigvalues();
services.AddSingleton(appConfig);
services.AddSingleton(appConfig.CosmoDb);

services
    .AddHttpClient<IJolpicaClient, JolpicaClient>(client =>
        {
            var jolpicaApiUrl = Environment.GetEnvironmentVariable("JolpicaBaseUrl", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(jolpicaApiUrl))
            {
                throw new InvalidOperationException("Configuration for JolpicaApi could not be loaded or BaseUrl is missing.");
            }
            client.BaseAddress = new Uri(jolpicaApiUrl);

            return new JolpicaClient()
            {
                ApiBase = jolpicaApiUrl
            };
        });

services.AddSingleton( s =>
{
    string connectionString = appConfig.CosmoDb.CosmosDbConnectionString;
    if (string.IsNullOrWhiteSpace(connectionString))
    { 
        throw new InvalidOperationException("The Cosmos Database connection was not found in appsettings"); 
    }

    CosmosSerializationOptions serializerOptions = new()
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
    };
    return new CosmosClientBuilder(connectionString)
    .WithSerializerOptions(serializerOptions)
    .Build();
});
// Register JolpicaService as an HTTP client
services.AddTransient<IJolpicaService, JolpicaService>();

// Register repositories
services.AddSingleton<ICosmoDataRepository, CosmosDataRepository>();
services.AddSingleton<IDriverRepository, DriverRepository>();
services.AddSingleton<IRaceRepository, RaceRepository>();
services.AddSingleton<IResultsRepository, ResultsRepository>();

// Register command handlers
services.AddTransient<IWriteDriversCommandHandler, WriteDriversCommandHandler>();
services.AddTransient<IWriteRoundsCommandHandler, WriteRoundsCommandHandler>();
services.AddTransient<IWriteResultsCommandHandler, WriteResultsCommandHandler>();

// Register AutoMapper
services.AddAutoMapper(typeof(DriverMappingProfile));

// Set environment variable for TimerTrigger
Environment.SetEnvironmentVariable("UpdateDatabaseCronSchedule", appConfig.UpdateDatabaseCronSchedule);

builder.Build().Run();

AppConfig GetAppConfigvalues()
{
    return new AppConfig()
    {
        AzureWebJobsStorage = GetEnvironmentVariableOrThrow("AzureWebJobsStorage"),
        UpdateDatabaseCronSchedule = GetEnvironmentVariableOrThrow("UpdateDatabaseCronSchedule"),
        Environment = GetEnvironmentVariableOrThrow("Environment"),
        CompetitionYear = GetEnvironmentVariableOrThrow("CompetitionYear"),
        CosmoDb = GetCosmoDbConfiguration()
    };
}

CosmoDbConfig GetCosmoDbConfiguration()
{
    return new CosmoDbConfig()
    {
        CosmosDbConnectionString = GetEnvironmentVariableOrThrow("CosmosDbConnectionString"),
        CosmosDbDatabaseId = GetEnvironmentVariableOrThrow("CosmosDbDatabaseId"),
        RetryCount = int.Parse(GetEnvironmentVariableOrThrow("CosmosDbRetryCount")),
        RetryTime = int.Parse(GetEnvironmentVariableOrThrow("CosmosDbRetryTime")),
        Containers = new ContainersConfig()
        {
            DriversContainer = GetEnvironmentVariableOrThrow("CosmosDbDriversContainer"),
            PreSeasonQuestionsContainer = GetEnvironmentVariableOrThrow("CosmosDbPreSeasonQuestionsContainer"),
            RacesContainer = GetEnvironmentVariableOrThrow("CosmosDbRacesContainer"),
            ResultsContainer = GetEnvironmentVariableOrThrow("CosmosDbResultsContainer"),
            SprintsContainer = GetEnvironmentVariableOrThrow("CosmosDbSprintsContainer"),
            UsersContainer = GetEnvironmentVariableOrThrow("CosmosDbUsersContainer")
        }
    };
}

string GetEnvironmentVariableOrThrow(string variableName)
{
    var value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);
    if (string.IsNullOrEmpty(value))
    {
        throw new InvalidOperationException($"Environment variable '{variableName}' is not set or is empty.");
    }
    return value;
}
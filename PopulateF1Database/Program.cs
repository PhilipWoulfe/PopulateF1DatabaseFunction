using JolpicaApi.Client;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PopulateF1Database.Config;
using PopulateF1Database.DataAccess.Interfaces;
using PopulateF1Database.DataAccess.Repositories;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var services = builder.Services;

// Register AppConfig as a singleton
AppConfig appConfig = GetAppConfigvalues();
services.AddSingleton(appConfig);

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

// Register JolpicaService as an HTTP client
services.AddHttpClient<IJolpicaService, JolpicaService>();

// Register CosmosDataRepository
services.AddSingleton<IDataRepository, CosmosDataRepository>();

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
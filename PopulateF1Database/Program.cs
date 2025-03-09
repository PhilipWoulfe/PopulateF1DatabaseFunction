using JolpicaApi.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PopulateF1Database.Config;
using PopulateF1Database.Data.Interfaces;
using PopulateF1Database.Data.Repositories;
using PopulateF1Database.Services.Interfaces;
using PopulateF1Database.Services.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Bind AppConfig and JolpicaApiConfig
        var appConfig = context.Configuration.GetSection("Values").Get<AppConfig>();
        if (appConfig == null)
        {
            throw new InvalidOperationException("Configuration for AppConfig could not be loaded.");
        }
        services.Configure<AppConfig>(context.Configuration.GetSection("Values"));
        services.Configure<JolpicaApiConfig>(context.Configuration.GetSection("JolpicaApi"));
        services.Configure<CosmoDbConfig>(context.Configuration.GetSection("CosmoDb"));

        // Register JolpicaClient
        services.AddHttpClient<IJolpicaClient, JolpicaClient>(client =>
        {
            var jolpicaApiConfig = context.Configuration.GetSection("JolpicaApi").Get<JolpicaApiConfig>();
            if (jolpicaApiConfig == null || string.IsNullOrEmpty(jolpicaApiConfig.BaseUrl))
            {
                throw new InvalidOperationException("Configuration for JolpicaApi could not be loaded or BaseUrl is missing.");
            }
            client.BaseAddress = new Uri(jolpicaApiConfig.BaseUrl);
        });

        // Register JolpicaService as an HTTP client
        services.AddHttpClient<IJolpicaService, JolpicaService>();

        // Register CosmosDataRepository
        services.AddSingleton<IDataRepository, CosmosDataRepository>();

        // Set environment variable for TimerTrigger
        Environment.SetEnvironmentVariable("UpdateDatabaseCronSchedule", appConfig.UpdateDatabaseCronSchedule);
    })
    .Build();

host.Run();

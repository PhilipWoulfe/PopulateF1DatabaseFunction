using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
        var appConfig = context.Configuration.GetSection("AppConfig").Get<AppConfig>();
        if (appConfig == null)
        {
            throw new InvalidOperationException("Configuration for AppConfig could not be loaded.");
        }
        services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
        services.Configure<JolpicaApiConfig>(context.Configuration.GetSection("JolpicaApi"));

        // Register JolpicaService as an HTTP client
        services.AddHttpClient<IJolpicaService, JolpicaService>();

        // Register CosmosDataRepository
        services.AddSingleton<IDataRepository, CosmosDataRepository>();

        // Set environment variable for TimerTrigger
        Environment.SetEnvironmentVariable("UpdateDatabaseCronSchedule", appConfig.UpdateDatabaseCronSchedule);
    })
    .Build();

host.Run();
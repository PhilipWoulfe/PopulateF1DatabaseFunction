using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PopulateF1Database.Config;
using PopulateF1Database.Data;

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
        services.Configure<AppConfig>(context.Configuration);
        services.AddSingleton<IDataRepository>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<AppConfig>>().Value;
            return new CosmosDataRepository(config.CosmosDBConnectionString, config.CosmosDBDatabaseId, config.CosmosDBContainerId);
        });

        // Set environment variable for TimerTrigger
        var config = context.Configuration.GetSection("Values").Get<AppConfig>();
        if (config is not null)
        {
            Environment.SetEnvironmentVariable("UpdateDatabaseCronSchedule", config.UpdateDatabaseCronSchedule);
        }
        else
        {
            throw new InvalidOperationException("Configuration for AppConfig could not be loaded.");
        }
    })
    .Build();

host.Run();
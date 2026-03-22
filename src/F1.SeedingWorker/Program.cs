using F1.Infrastructure.Data;
using F1.SeedingWorker;
using F1.SeedingWorker.Clients;
using F1.SeedingWorker.Options;
using F1.SeedingWorker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddOptions<SeedOptions>()
	.Bind(builder.Configuration.GetSection(SeedOptions.SectionName))
	.ValidateDataAnnotations()
	.ValidateOnStart();

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(postgresConnectionString))
{
	throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");
}

builder.Services.AddDbContextFactory<F1DbContext>(options => options.UseNpgsql(postgresConnectionString));

builder.Services
	.AddHttpClient<IJolpicaClient, JolpicaClient>((sp, client) =>
	{
		var seedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SeedOptions>>().Value;
		client.BaseAddress = new Uri(seedOptions.JolpicaBaseUrl, UriKind.Absolute);
	});

builder.Services.AddSingleton<ISeedOrchestrator, SeedOrchestrator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();

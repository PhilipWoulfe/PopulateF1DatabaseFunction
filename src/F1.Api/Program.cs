using F1.Api.Middleware;
using Serilog;
using Serilog.Formatting.Compact;
using F1.Api.Services;
using F1.Core.Interfaces;
using F1.Infrastructure.Repositories;
using F1.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, logConfig) =>
{
    logConfig
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext();

    logConfig.WriteTo.Console(new CompactJsonFormatter());

    var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");
    if (!string.IsNullOrWhiteSpace(logFilePath))
    {
        logConfig.WriteTo.File(
            new CompactJsonFormatter(),
            Path.Combine(logFilePath, "f1api-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true);
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers(); // Add this line to register controller services
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IRaceMetadataService, RaceMetadataService>();
builder.Services.AddScoped<ISelectionService, SelectionService>();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddMemoryCache();
builder.Services
    .AddOptions<CloudflareAccessOptions>()
    .Bind(builder.Configuration.GetSection("CloudflareAccess"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "CloudflareAccess:Issuer must be configured.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "CloudflareAccess:Audience must be configured.")
    .ValidateOnStart();
builder.Services.AddHttpClient<ICloudflareJwtValidator, CloudflareJwtValidator>();

builder.Services.AddSingleton((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CosmosDb:ConnectionString"];
    return new CosmosClient(connectionString);
});
builder.Services.AddScoped<IDriverRepository, CosmosDriverRepository>();
builder.Services.AddScoped<IRaceMetadataRepository, CosmosRaceMetadataRepository>();
builder.Services.AddScoped<ISelectionRepository, CosmosSelectionRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorOrigin",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Host == "localhost")
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }
            else
            {
                var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
                policy.WithOrigins(origins)
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }
        });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swagger, httpReq) =>
        {
            // If accessed via Nginx (which adds X-Forwarded-For), tell Swagger we are at /api
            if (httpReq.Headers.ContainsKey("X-Forwarded-For"))
            {
                swagger.Servers = new List<OpenApiServer> { new() { Url = "/api" } };
            }
        });
    });
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorOrigin");

app.UseMiddleware<CloudflareAccessMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // This line is crucial for mapping your controllers

app.MapGet("/races/results", (IRaceService raceService) =>
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
}).RequireAuthorization();

app.Run();



public partial class Program { }

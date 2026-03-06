using F1.Api.Middleware;
using F1.Api.Services;
using F1.Core.Interfaces;
using F1.Infrastructure.Repositories;
using F1.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers(); // Add this line to register controller services
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<ISelectionService, SelectionService>();
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddMemoryCache();
builder.Services.Configure<CloudflareAccessOptions>(builder.Configuration.GetSection("CloudflareAccess"));
builder.Services.AddHttpClient<ICloudflareJwtValidator, CloudflareJwtValidator>();

builder.Services.AddSingleton((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CosmosDb:ConnectionString"];
    return new CosmosClient(connectionString);
});
builder.Services.AddScoped<IDriverRepository, CosmosDriverRepository>();
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

app.Use(async (context, next) =>
{
    app.Logger.LogInformation("DEBUG: Request {Method} {Path}", context.Request.Method, context.Request.Path);

    if (context.Request.Headers.ContainsKey("Cf-Access-Jwt-Assertion"))
    {
        app.Logger.LogDebug("DEBUG: Found Cloudflare Access JWT assertion header.");
    }
    else
    {
        app.Logger.LogWarning("DEBUG: Cloudflare Access JWT assertion header missing.");
    }

    await next();
});

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

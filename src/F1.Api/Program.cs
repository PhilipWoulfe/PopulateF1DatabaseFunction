using F1.Api.Middleware;
using F1.Core.Interfaces;
using F1.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRaceService, RaceService>();
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

var simulateCloudflare = builder.Configuration.GetValue<bool>("DevSettings:SimulateCloudflare");
if (app.Environment.IsDevelopment() && simulateCloudflare)
{
    app.Use((context, next) =>
    {
        var mockEmail = builder.Configuration.GetValue<string>("DevSettings:MockEmail") ?? "dev-user@example.com";
        context.Request.Headers["Cf-Access-Authenticated-User-Email"] = mockEmail;
        return next();
    });
}

app.UseCors("AllowBlazorOrigin");

app.Use(async (context, next) =>
{
    app.Logger.LogInformation("DEBUG: Request {Method} {Path}", context.Request.Method, context.Request.Path);

    if (context.Request.Headers.TryGetValue("Cf-Access-Authenticated-User-Email", out var email))
    {
        app.Logger.LogInformation("DEBUG: Found Cloudflare Auth Header: {Email}", email);
    }
    else
    {
        app.Logger.LogWarning("DEBUG: Cloudflare Auth Header MISSING. Available Headers: {Headers}", string.Join(", ", context.Request.Headers.Keys));
    }

    await next();
});

app.UseMiddleware<CloudflareAccessMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/races/results", (IRaceService raceService) => 
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
}).RequireAuthorization();

app.Run();



public partial class Program { }

using F1.Core.Interfaces;
using F1.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the Service (The "Wiring")
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorOrigin",
        policy =>
        {
            // Change "AllowedOrigins" to match your .env.local variable name
            var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
            policy.WithOrigins(origins)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowBlazorOrigin");

// 2. Map the Endpoint
app.MapGet("/races/results", (IRaceService raceService) => 
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
});

app.Run();
using F1.Core.Interfaces;
using F1.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the Service (The "Wiring")
builder.Services.AddScoped<IRaceService, RaceService>();

var app = builder.Build();

// 2. Map the Endpoint
app.MapGet("/races/results", (IRaceService raceService) => 
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
});

app.Run();
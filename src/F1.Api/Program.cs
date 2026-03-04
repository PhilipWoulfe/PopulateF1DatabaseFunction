using F1.Core.Interfaces;
using F1.Services;
using F1.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the Service (The "Wiring")
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IUserContext, UserContext>();
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

if (app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Headers["Cf-Access-Authenticated-User-Email"] = "dev@example.com";
        return next();
    });
}

app.UseCors("AllowBlazorOrigin");

// 2. Map the Endpoint
app.MapGet("/races/results", (IRaceService raceService) => 
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
});

app.MapGet("/api/me", (IUserContext userContext) => {
    var user = userContext.GetCurrentUser();
    if (user == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(user);
});

app.Run();
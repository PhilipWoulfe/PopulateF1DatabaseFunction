using F1.Api.Middleware;
using F1.Core.Interfaces;
using F1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorOrigin",
        policy =>
        {
            var origins = builder.Configuration["AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
            policy.WithOrigins(origins)
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

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

app.UseMiddleware<CloudflareAccessMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/races/results", (IRaceService raceService) => 
{
    var results = raceService.GetMockResults();
    return Results.Ok(results);
}).RequireAuthorization();

app.Run();
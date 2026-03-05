using F1.Web;
using F1.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- This is the fix for the API client ---
// Register a transient handler for mocking Cloudflare headers in development.
builder.Services.AddTransient<DevMockAuthHandler>();

builder.Services.AddHttpClient("F1Api", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var nav = sp.GetRequiredService<NavigationManager>();

    // Access the nested value
    var apiBaseUrl = config["F1Api:BaseUrl"];

    if (!string.IsNullOrWhiteSpace(apiBaseUrl) && Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var absoluteUri))
    {
        client.BaseAddress = absoluteUri;
    }
    else
    {
        // Absolute fallback if appsettings is missing or broken
        client.BaseAddress = new Uri(nav.BaseUri.EndsWith("/") ? nav.BaseUri + "api/" : nav.BaseUri + "/api/");
    }

    Console.WriteLine($"🚀 F1Api Final Address: {client.BaseAddress}");
})
.AddHttpMessageHandler<DevMockAuthHandler>();

// Register the main HttpClient for dependency injection. It will create clients using the "F1Api" configuration.
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("F1Api"));


// --- Inferred Services from your Layout/Nav components ---
builder.Services.AddScoped<IUserSession, UserSession>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();

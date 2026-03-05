using F1.Web;
using F1.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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

// Register a named HttpClient for your API.
builder.Services.AddHttpClient("F1Api", (sp, client) =>
{
    // Use the NavigationManager to get the app's base URI (e.g., http://localhost:5001/).
    // This ensures we have an absolute path, preventing the "file:///" error.
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    client.BaseAddress = new Uri(new Uri(navigationManager.BaseUri), "api/");
})
.AddHttpMessageHandler<DevMockAuthHandler>(); // This attaches your mock header handler.

// Register the main HttpClient for dependency injection. It will create clients using the "F1Api" configuration.
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("F1Api"));


// --- Inferred Services from your Layout/Nav components ---
builder.Services.AddScoped<IUserSession, UserSession>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

await builder.Build().RunAsync();

using F1.Web;
using F1.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- This is the fix for the API client ---
// Register a transient handler for mocking Cloudflare headers in development.
builder.Services.AddTransient<DevMockAuthHandler>();

const string ClientName = "F1Api";

// Define the API configuration logic once to be used by multiple registrations
void ConfigureApi(IServiceProvider sp, HttpClient client)
{
    var config = sp.GetRequiredService<IConfiguration>();
    var nav = sp.GetRequiredService<NavigationManager>();
    var apiBaseUrl = config["F1Api:BaseUrl"];

    if (!string.IsNullOrWhiteSpace(apiBaseUrl) && Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var absoluteUri))
    {
        client.BaseAddress = absoluteUri;
    }
    else
    {
        client.BaseAddress = new Uri(nav.BaseUri.EndsWith("/") ? nav.BaseUri + "api/" : nav.BaseUri + "/api/");
    }

    Console.WriteLine($"🚀 API Address: {client.BaseAddress}");
}

// 1. Register the named client configuration
builder.Services.AddHttpClient(ClientName, ConfigureApi)
    .AddHttpMessageHandler<DevMockAuthHandler>();

// 2. Register the default HttpClient for Razor components (e.g. @inject HttpClient)
// This also allows UserSession to simply ask for 'HttpClient' in its constructor
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName));

// 3. Register UserSession as SCOPED
builder.Services.AddScoped<IUserSession, UserSession>();

// --- Auth Services ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

var host = builder.Build();

// Load the authenticated user session once at startup so layout/pages can render immediately.
var userSession = host.Services.GetRequiredService<IUserSession>();
await userSession.InitializeAsync();

await host.RunAsync();

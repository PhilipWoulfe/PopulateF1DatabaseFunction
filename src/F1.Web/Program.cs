using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using F1.Web;
using F1.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

// 2. Print it to the browser console
// You'll see this in the Chrome/Firefox F12 Console tab
Console.WriteLine($"DEBUG: ApiBaseUrl from config is: '{apiBaseUrl}'");

// 3. (Optional) Check the environment name too
Console.WriteLine($"DEBUG: Current Environment is: {builder.HostEnvironment.Environment}");

builder.Services.AddTransient<DevMockAuthHandler>();

// Use the value (with the safety check we discussed earlier)
builder.Services.AddHttpClient("F1Api", client => 
{
    client.BaseAddress = new Uri(apiBaseUrl ?? builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<DevMockAuthHandler>();

// 2. Add this line so @inject HttpClient works in your .razor files
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("F1Api"));

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<IUserSession, UserSession>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();


var host = builder.Build();

var userSession = host.Services.GetRequiredService<IUserSession>();
await userSession.InitializeAsync();

await host.RunAsync();


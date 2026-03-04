using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using F1.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

// 2. Print it to the browser console
// You'll see this in the Chrome/Firefox F12 Console tab
Console.WriteLine($"DEBUG: ApiBaseUrl from config is: '{apiBaseUrl}'");

// 3. (Optional) Check the environment name too
Console.WriteLine($"DEBUG: Current Environment is: {builder.HostEnvironment.Environment}");

// Use the value (with the safety check we discussed earlier)
builder.Services.AddHttpClient("F1Api", client => 
{
    client.BaseAddress = new Uri(apiBaseUrl ?? builder.HostEnvironment.BaseAddress);
});

// 2. Add this line so @inject HttpClient works in your .razor files
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("F1Api"));

await builder.Build().RunAsync();

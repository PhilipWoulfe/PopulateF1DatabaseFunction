using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using F1.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("F1Api", client => 
{
    var baseUrl = builder.Configuration["F1Api:BaseUrl"] 
        ?? "http://localhost:5000"; // Fallback for local dev
    client.BaseAddress = new Uri(baseUrl);
});

// 2. Add this line so @inject HttpClient works in your .razor files
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("F1Api"));

await builder.Build().RunAsync();

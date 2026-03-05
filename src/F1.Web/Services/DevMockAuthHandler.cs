using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;

namespace F1.Web.Services
{
    public class DevMockAuthHandler : DelegatingHandler
    {
        private readonly IWebAssemblyHostEnvironment _env;
        private readonly IConfiguration _config;

        public DevMockAuthHandler(IWebAssemblyHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Check if we are in Dev and if the mock is enabled in appsettings.json
            var simulate = _config.GetValue<bool>("DevSettings:SimulateCloudflare");
            var mockEmail = _config.GetValue<string>("DevSettings:MockEmail") ?? "dev-user@example.com";

            if (_env.IsDevelopment() && simulate)
            {
                // Inject the headers that Cloudflare Access would usually provide
                if (!request.Headers.Contains("Cf-Access-Authenticated-User-Email"))
                {
                    request.Headers.Add("Cf-Access-Authenticated-User-Email", mockEmail);
                }
                
                // Optional: Mock the JWT assertion header if your API validates signatures
                if (!request.Headers.Contains("Cf-Access-Jwt-Assertion"))
                {
                    request.Headers.Add("Cf-Access-Jwt-Assertion", "mock-local-jwt-token");
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Linq;

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
            var simulate = _config.GetValue<bool>("DevSettings:SimulateCloudflare");
            var mockEmail = _config.GetValue<string>("DevSettings:MockEmail") ?? "dev-user@example.com";
            var mockGroups = ReadConfiguredValues("DevSettings:MockGroups");
            var injectMockJwt = _config.GetValue<bool>("DevSettings:InjectMockJwt");

            if (_env.IsDevelopment() && simulate)
            {
                if (!request.Headers.Contains("Cf-Access-Authenticated-User-Email"))
                {
                    request.Headers.Add("Cf-Access-Authenticated-User-Email", mockEmail);
                }

                if (mockGroups.Count > 0 && !request.Headers.Contains("Cf-Access-Mock-Groups"))
                {
                    request.Headers.Add("Cf-Access-Mock-Groups", string.Join(',', mockGroups));
                }

                if (injectMockJwt && !request.Headers.Contains("Cf-Access-Jwt-Assertion"))
                {
                    request.Headers.Add("Cf-Access-Jwt-Assertion", "mock-local-jwt-token");
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private IReadOnlyList<string> ReadConfiguredValues(string sectionPath)
        {
            var section = _config.GetSection(sectionPath);
            var children = section.GetChildren()
                .Select(child => child.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim());

            var scalar = section.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            return children
                .Concat(scalar)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

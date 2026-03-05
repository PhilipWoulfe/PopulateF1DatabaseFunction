using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace F1.Api.Tests.Integration
{
    public class RacesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RacesEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetRacesResults_ShouldReturnUnauthorized_WhenSimulateCloudflareIsFalse()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "DevSettings:SimulateCloudflare", "false" }
                    });
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/races/results");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetRacesResults_ShouldReturnOk_WhenSimulateCloudflareIsTrue()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "DevSettings:SimulateCloudflare", "true" }
                    });
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/races/results");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}

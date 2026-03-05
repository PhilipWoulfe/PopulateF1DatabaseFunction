using F1.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace F1.Api.Tests.Middleware
{
    public class CloudflareAccessMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldReturnUnauthorized_WhenHeaderIsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) =>
            {
                // This should not be called
                Assert.Fail("The next middleware should not be called.");
                return Task.CompletedTask;
            }, configuration);

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCallNextMiddleware_WhenHeaderIsPresent()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var nextCalled = false;
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            }, configuration);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "test@example.com";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddEmailClaim_WhenHeaderIsPresent()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "test@example.com";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(httpContext.User.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == "test@example.com"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddAdminRoleClaim_WhenUserIsAdmin()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "philip.woulfe@gmail.com";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldSimulateCloudflare_WhenDevSettingIsTrue()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"DevSettings:SimulateCloudflare", "true"},
                {"DevSettings:MockEmail", "dev-user@example.com"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var nextCalled = false;
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            }, configuration);

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(nextCalled);
            Assert.True(httpContext.Request.Headers.ContainsKey("Cf-Access-Authenticated-User-Email"));
            Assert.Equal("dev-user@example.com", httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"]);
        }
    }
}

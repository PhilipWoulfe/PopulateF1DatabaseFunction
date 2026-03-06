using F1.Api.Middleware;
using F1.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace F1.Api.Tests.Middleware
{
    public class CloudflareAccessMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ShouldReturnUnauthorized_WhenJwtHeaderIsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var validator = new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(new ClaimsPrincipal()));
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) =>
            {
                // This should not be called
                Assert.Fail("The next middleware should not be called.");
                return Task.CompletedTask;
            }, configuration, validator);

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCallNextMiddleware_WhenJwtIsValid()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var nextCalled = false;
            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("test@example.com", "Test User", "sub-123"))
            );

            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            }, configuration, validator);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddEmailClaim_WhenJwtIsValid()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("test@example.com", "Test User", "sub-123"))
            );

            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration, validator);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

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
            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("philip.woulfe@gmail.com", "Philip", "admin-1"))
            );
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration, validator);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldSimulateCloudflare_WhenDevSettingIsTrue()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?> {
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
            }, configuration, new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not used")));

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(nextCalled);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "dev-user@example.com"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnUnauthorized_WhenValidatorRejectsToken()
        {
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("Unauthorized: invalid Cloudflare Access token."))
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "invalid.token";

            await middleware.InvokeAsync(httpContext);

            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAllowLegacyHeader_WhenBypassEnabled()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"CloudflareAccess:AllowLegacyHeaderBypass", "true"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var nextCalled = false;
            var middleware = new CloudflareAccessMiddleware(
                next: _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not used"))
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "legacy@example.com";

            await middleware.InvokeAsync(httpContext);

            Assert.True(nextCalled);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "legacy@example.com"));
        }

        private static ClaimsPrincipal CreatePrincipal(string email, string name, string subject)
        {
            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("email", email),
                        new Claim("name", name),
                        new Claim("sub", subject)
                    },
                    "CloudflareJwt"
                )
            );
        }

        private sealed class FakeCloudflareJwtValidator : ICloudflareJwtValidator
        {
            private readonly Func<string, CloudflareTokenValidationResult> _resultFactory;

            public FakeCloudflareJwtValidator(Func<string, CloudflareTokenValidationResult> resultFactory)
            {
                _resultFactory = resultFactory;
            }

            public Task<CloudflareTokenValidationResult> ValidateAsync(string jwtAssertion, CancellationToken cancellationToken)
            {
                return Task.FromResult(_resultFactory(jwtAssertion));
            }
        }
    }
}

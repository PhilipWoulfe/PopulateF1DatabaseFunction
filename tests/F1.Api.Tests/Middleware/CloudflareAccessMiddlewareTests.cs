using F1.Api.Middleware;
using F1.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            }, configuration, validator, CreateHostEnvironment(Environments.Production));

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
            }, configuration, validator, CreateHostEnvironment(Environments.Production), new CapturingLogger<CloudflareAccessMiddleware>());

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

            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(httpContext.User.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == "test@example.com"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotAddAdminRoleClaim_WhenNoAdminGroupsAreConfigured()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();

            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("admin@testemail.com", "Philip", "admin-1"))
            );
            var middleware = new CloudflareAccessMiddleware(next: (innerHttpContext) => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.False(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddAdminRoleClaim_WhenConfiguredGroupClaimMatches()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CloudflareAccess:AdminGroupClaimType"] = "groups",
                    ["CloudflareAccess:AdminGroups:0"] = "F1 Admins"
                })
                .Build();

            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("user@example.com", "User", "sub-123", [new Claim("groups", "F1 Admins")]))
            );

            var middleware = new CloudflareAccessMiddleware(next: _ => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.True(httpContext.User.IsInRole("Admin"));
            Assert.True(httpContext.User.HasClaim("groups", "F1 Admins"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddAdminRoleClaim_WhenConfiguredAdminEmailMatches()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CloudflareAccess:AdminGroups:0"] = "F1 Admins",
                    ["CloudflareAccess:AdminEmails:0"] = "admin@example.com"
                })
                .Build();

            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("admin@example.com", "Admin User", "sub-123", [new Claim("groups", "F1 Users")]))
            );

            var middleware = new CloudflareAccessMiddleware(next: _ => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.True(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotAddAdminRoleClaim_WhenConfiguredAdminEmailDoesNotMatch()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CloudflareAccess:AdminGroups:0"] = "F1 Admins",
                    ["CloudflareAccess:AdminEmails:0"] = "admin@example.com"
                })
                .Build();

            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("user@example.com", "Normal User", "sub-123", [new Claim("groups", "F1 Users")]))
            );

            var middleware = new CloudflareAccessMiddleware(next: _ => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.False(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddAdminRoleClaim_WhenConfiguredAdminEmailMatchesCaseInsensitive()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CloudflareAccess:AdminEmails:0"] = "ADMIN@EXAMPLE.COM"
                })
                .Build();

            var validator = new FakeCloudflareJwtValidator(_ =>
                CloudflareTokenValidationResult.Success(CreatePrincipal("admin@example.com", "Admin User", "sub-123"))
            );

            var middleware = new CloudflareAccessMiddleware(next: _ => Task.CompletedTask, configuration, validator, CreateHostEnvironment(Environments.Production));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

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
            }, configuration, new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not_used")), CreateHostEnvironment(Environments.Development));

            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(nextCalled);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "dev-user@example.com"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldGrantAdminRole_WhenDevMockGroupsContainConfiguredAdminGroup()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"DevSettings:SimulateCloudflare", "true"},
                {"DevSettings:MockEmail", "dev-user@example.com"},
                {"DevSettings:MockGroups:0", "F1 Admins"},
                {"DevSettings:MockGroups:1", "F1 Users"},
                {"CloudflareAccess:AdminGroupClaimType", "groups"},
                {"CloudflareAccess:AdminGroups:0", "F1 Admins"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not_used")),
                CreateHostEnvironment(Environments.Development));

            var httpContext = new DefaultHttpContext();

            await middleware.InvokeAsync(httpContext);

            Assert.True(httpContext.User.IsInRole("Admin"));
            Assert.True(httpContext.User.HasClaim("groups", "F1 Admins"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotSimulateCloudflare_WhenNotInDevelopment()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"DevSettings:SimulateCloudflare", "true"},
                {"DevSettings:MockEmail", "dev-user@example.com"}
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
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not_used")),
                CreateHostEnvironment(Environments.Production)
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            Assert.False(nextCalled);
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            Assert.Equal("Unauthorized.", responseBody);
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnUnauthorized_WhenValidatorRejectsToken()
        {
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("token_invalid")),
                CreateHostEnvironment(Environments.Production)
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
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("not_used")),
                CreateHostEnvironment(Environments.Development)
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "legacy@example.com";

            await middleware.InvokeAsync(httpContext);

            Assert.True(nextCalled);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "legacy@example.com"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnUnauthorized_WhenValidatedPrincipalMissingEmailClaim()
        {
            var configuration = new ConfigurationBuilder().Build();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ =>
                    CloudflareTokenValidationResult.Success(
                        new ClaimsPrincipal(
                            new ClaimsIdentity(
                                new[]
                                {
                                    new Claim("name", "Test User"),
                                    new Claim("sub", "sub-123")
                                },
                                "CloudflareJwt"
                            )
                        )
                    )
                ),
                CreateHostEnvironment(Environments.Production)
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";
            httpContext.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            using var reader = new StreamReader(httpContext.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
            Assert.Equal("Unauthorized.", responseBody);
        }

        [Fact]
        public async Task InvokeAsync_ShouldUseServiceTokenFallbackIdentity_InTestEnvironment_WhenAllowlisted()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "token-sub-123",
                ["CloudflareAccess:TestServiceTokenEmailDomain"] = "e2e.local"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var nextCalled = false;
            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("sub", "token-sub-123"),
                        new Claim("common_name", "CI Service Token")
                    },
                    "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test")
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.True(nextCalled);
            Assert.True(httpContext.User.Identity?.IsAuthenticated ?? false);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "token-sub-123@e2e.local"));
            Assert.False(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotUseServiceTokenFallbackIdentity_WhenSubjectNotAllowlisted()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "different-subject"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("sub", "token-sub-123") },
                    "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test")
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";
            httpContext.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(httpContext);

            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ShouldGrantAdminRoleViaServiceTokenFallback_WhenAdminSubjectAllowlisted()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "token-sub-123",
                ["CloudflareAccess:TestServiceTokenAdminSubjectAllowlist:0"] = "token-sub-123"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("sub", "token-sub-123") },
                    "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test")
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.True(httpContext.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task InvokeAsync_ShouldUseServiceTokenFallback_WhenCommonNameMatchesAllowlist()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "d49e39507f256c184afceafc7048e6e7.access"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("common_name", "d49e39507f256c184afceafc7048e6e7.access") },
                    "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test")
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
            Assert.True(httpContext.User.Identity?.IsAuthenticated ?? false);
            Assert.True(httpContext.User.HasClaim(c => c.Type == ClaimTypes.Email));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotUseServiceTokenFallbackIdentity_OutsideTestEnvironment()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "token-sub-123"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("sub", "token-sub-123") },
                    "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment(Environments.Production)
            );

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }

        private static ClaimsPrincipal CreatePrincipal(string email, string name, string subject, IEnumerable<Claim>? additionalClaims = null)
        {
            var claims = new List<Claim>
            {
                new Claim("email", email),
                new Claim("name", name),
                new Claim("sub", subject)
            };

            if (additionalClaims is not null)
            {
                claims.AddRange(additionalClaims);
            }

            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
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

        private static IHostEnvironment CreateHostEnvironment(string environmentName)
        {
            return new FakeHostEnvironment(environmentName);
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public FakeHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
                ApplicationName = "F1.Api.Tests";
                ContentRootPath = Directory.GetCurrentDirectory();
                ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; }

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public readonly List<(LogLevel Level, string Message)> Entries = new();

            IDisposable? ILogger.BeginScope<TState>(TState state) => null;
            bool ILogger.IsEnabled(LogLevel logLevel) => true;
            void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogWarning_WithMissingJwtHeaderReasonCode_WhenJwtHeaderAbsent()
        {
            var configuration = new ConfigurationBuilder().Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => { Assert.Fail("Next should not be called."); return Task.CompletedTask; },
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(new ClaimsPrincipal())),
                CreateHostEnvironment(Environments.Production),
                logger);

            await middleware.InvokeAsync(new DefaultHttpContext());

            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("missing_jwt_header"));
            Assert.Equal(1, logger.Entries.Count(e => e.Level == LogLevel.Warning));
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogWarning_WithValidatorReasonCode_WhenValidatorRejectsToken()
        {
            var configuration = new ConfigurationBuilder().Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Failure("token_invalid", kidPresent: true)),
                CreateHostEnvironment(Environments.Production),
                logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.sig";

            await middleware.InvokeAsync(httpContext);

            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("token_invalid"));
            Assert.Equal(1, logger.Entries.Count(e => e.Level == LogLevel.Warning));
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogWarning_WithMissingEmailClaimReasonCode_WhenEmailClaimAbsent()
        {
            var configuration = new ConfigurationBuilder().Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("name", "No Email"), new Claim("sub", "sub-1") },
                    "CloudflareJwt"));
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment(Environments.Production),
                logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.sig";

            await middleware.InvokeAsync(httpContext);

            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("missing_email_claim"));
            Assert.Equal(1, logger.Entries.Count(e => e.Level == LogLevel.Warning));
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotLogWarning_WhenAuthSucceeds()
        {
            var configuration = new ConfigurationBuilder().Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var middleware = new CloudflareAccessMiddleware(
                next: _ => Task.CompletedTask,
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(CreatePrincipal("user@example.com", "User", "sub-1"))),
                CreateHostEnvironment(Environments.Production),
                logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.sig";

            await middleware.InvokeAsync(httpContext);

            Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogFallbackWarning_WithNon200DownstreamStatus()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "token-sub-123"
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("sub", "token-sub-123") }, "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; },
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test"),
                logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await middleware.InvokeAsync(httpContext);

            var warning = logger.Entries.FirstOrDefault(e => e.Level == LogLevel.Warning && e.Message.Contains("service_token_email_fallback"));
            Assert.Contains("StatusCode=403", warning.Message);
        }

        [Fact]
        public async Task InvokeAsync_ShouldLogFallbackWarning_WithExceptionAndStatus500()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["CloudflareAccess:EnableTestServiceTokenFallback"] = "true",
                ["CloudflareAccess:TestServiceTokenSubjectAllowlist:0"] = "token-sub-123"
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var logger = new CapturingLogger<CloudflareAccessMiddleware>();
            var principalWithoutEmail = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("sub", "token-sub-123") }, "CloudflareJwt"));

            var middleware = new CloudflareAccessMiddleware(
                next: ctx => throw new InvalidOperationException("Downstream error!"),
                configuration,
                new FakeCloudflareJwtValidator(_ => CloudflareTokenValidationResult.Success(principalWithoutEmail)),
                CreateHostEnvironment("Test"),
                logger);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Jwt-Assertion"] = "header.payload.signature";

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(httpContext));

            var warning = logger.Entries.FirstOrDefault(e => e.Level == LogLevel.Warning && e.Message.Contains("service_token_email_fallback"));
            Assert.Contains("StatusCode=500", warning.Message);
        }
    }
}

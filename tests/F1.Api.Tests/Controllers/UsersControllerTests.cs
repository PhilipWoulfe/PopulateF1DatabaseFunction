using F1.Api.Controllers;
using F1.Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;


namespace F1.Api.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public void GetMe_WithValidCloudflareHeaders_ReturnsOkWithUserDetails()
        {
            // Arrange
            var controller = CreateController();

            // Mock the HttpContext user claims set by middleware.
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Email, "driver@f1.com"),
                        new Claim(ClaimTypes.Name, "driver"),
                        new Claim(ClaimTypes.NameIdentifier, "user-guid-123"),
                        new Claim(ClaimTypes.Role, "Admin")
                    },
                    "Cloudflare"
                )
            );

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // Act
            ActionResult<UserDto> result = controller.GetMe();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var userDto = Assert.IsType<UserDto>(okResult.Value);

            Assert.Equal("driver@f1.com", userDto.Email);
            Assert.Equal("user-guid-123", userDto.Id);
            Assert.True(userDto.IsAdmin);
        }

        [Fact]
        public void GetMe_MissingCloudflareHeaders_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateController();
            var httpContext = new DefaultHttpContext();
            // No headers set

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // Act
            ActionResult<UserDto> result = controller.GetMe();

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public void GetDebugMe_WhenEnabledAndAuthenticated_ReturnsSanitizedDebugPayload()
        {
            var controller = CreateController(
                new Dictionary<string, string?>
                {
                    ["DevSettings:EnableDebugEndpoints"] = "true",
                    ["CloudflareAccess:AdminGroupClaimType"] = "groups",
                    ["CloudflareAccess:AdminGroups:0"] = "F1 Admins"
                });

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Email, "driver@f1.com"),
                        new Claim(ClaimTypes.Name, "driver"),
                        new Claim(ClaimTypes.NameIdentifier, "user-guid-123"),
                        new Claim("groups", "F1 Admins"),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim("group", new string('A', 180)),
                        new Claim("custom-sensitive", "should-not-appear")
                    },
                    "Cloudflare"
                )
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            ActionResult<UserDebugDto> result = controller.GetDebugMe();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<UserDebugDto>(okResult.Value);

            Assert.True(payload.IsAuthenticated);
            Assert.True(payload.IsAdmin);
            Assert.Equal("Cloudflare", payload.AuthenticationType);
            Assert.Equal("***@f1.com", payload.Email);
            Assert.Equal("us***23", payload.Id);
            Assert.Equal(string.Empty, payload.Name);
            Assert.Contains("F1 Admins", payload.Groups);
            Assert.Contains("Admin", payload.Roles);
            Assert.Contains(payload.Claims, claim => claim.Type == "groups" && claim.Value == "F1 Admins");
            Assert.Contains(payload.Claims, claim => claim.Type == "group" && claim.Value.EndsWith("...(truncated)", StringComparison.Ordinal));
            Assert.Contains(payload.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == "***@f1.com");
            Assert.Contains(payload.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == "us***23");
            Assert.DoesNotContain(payload.Claims, claim => claim.Type == "custom-sensitive");
        }

        [Fact]
        public void GetDebugMe_WhenDisabled_ReturnsNotFound()
        {
            var controller = CreateController(new Dictionary<string, string?>
            {
                ["DevSettings:EnableDebugEndpoints"] = "false"
            });

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateAuthenticatedHttpContext()
            };

            ActionResult<UserDebugDto> result = controller.GetDebugMe();

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetDebugMe_WhenEnvironmentIsProduction_ReturnsNotFound()
        {
            var controller = CreateController(
                new Dictionary<string, string?>
                {
                    ["DevSettings:EnableDebugEndpoints"] = "true"
                },
                Environments.Production);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateAuthenticatedHttpContext()
            };

            ActionResult<UserDebugDto> result = controller.GetDebugMe();

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetDebugMe_WhenEnabledButUnauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(new Dictionary<string, string?>
            {
                ["DevSettings:EnableDebugEndpoints"] = "true"
            });

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            ActionResult<UserDebugDto> result = controller.GetDebugMe();

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        private static UsersController CreateController(
            IDictionary<string, string?>? config = null,
            string environmentName = "Development")
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(config ?? new Dictionary<string, string?>())
                .Build();

            return new UsersController(configuration, CreateHostEnvironment(environmentName));
        }

        private static DefaultHttpContext CreateAuthenticatedHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Email, "driver@f1.com"),
                        new Claim(ClaimTypes.Name, "driver"),
                        new Claim(ClaimTypes.NameIdentifier, "user-guid-123")
                    },
                    "Cloudflare"
                )
            );

            return httpContext;
        }

        private static IHostEnvironment CreateHostEnvironment(string environmentName)
        {
            return new FakeHostEnvironment { EnvironmentName = environmentName };
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "F1.Api.Tests";
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}

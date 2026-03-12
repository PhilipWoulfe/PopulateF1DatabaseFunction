using F1.Api.Controllers;
using F1.Core.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace F1.Api.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public void GetMe_WithValidCloudflareHeaders_ReturnsOkWithUserDetails()
        {
            // Arrange
            var controller = new UsersController();

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
            var controller = new UsersController();
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
    }
}

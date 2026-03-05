using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using F1.Api.Controllers;
using F1.Core.Dtos;


namespace F1.Api.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public void GetMe_WithValidCloudflareHeaders_ReturnsOkWithUserDetails()
        {
            // Arrange
            var controller = new UsersController();
            
            // Mock the HttpContext and Headers
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "driver@f1.com";
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Id"] = "user-guid-123";

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

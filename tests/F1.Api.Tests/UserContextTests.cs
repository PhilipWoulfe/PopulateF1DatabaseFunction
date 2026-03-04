using F1.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Api.Tests
{
    public class UserContextTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly UserContext _userContext;

        public UserContextTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _configurationMock = new Mock<IConfiguration>();
            _userContext = new UserContext(_httpContextAccessorMock.Object, _configurationMock.Object);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnNull_WhenHeaderIsMissing()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = _userContext.GetCurrentUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnUser_WhenHeaderIsPresent()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "test@example.com";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            _configurationMock.Setup(x => x["AdminEmail"]).Returns("admin@example.com");

            // Act
            var result = _userContext.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.False(result.IsAdmin);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnAdminUser_WhenEmailMatchesAdminEmail()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Cf-Access-Authenticated-User-Email"] = "admin@example.com";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
            _configurationMock.Setup(x => x["AdminEmail"]).Returns("admin@example.com");

            // Act
            var result = _userContext.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin@example.com", result.Email);
            Assert.True(result.IsAdmin);
        }
    }
}

using F1.Web.Models;
using F1.Web.Services;
using Moq;
using System.ComponentModel;
using System.Security.Claims;

namespace F1.Web.Tests.Services
{
    public class CustomAuthenticationStateProviderTests : TestContext
    {
        private readonly Mock<IUserSession> _userSessionMock;
        private readonly CustomAuthenticationStateProvider _authenticationStateProvider;

        public CustomAuthenticationStateProviderTests()
        {
            _userSessionMock = new Mock<IUserSession>();
            _authenticationStateProvider = new CustomAuthenticationStateProvider(_userSessionMock.Object);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAnonymous_WhenUserIsNull()
        {
            // Arrange
            _userSessionMock.Setup(x => x.User).Returns((User?)null);

            // Act
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            Assert.False(authState.User.Identity?.IsAuthenticated ?? false);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAnonymous_WhenUserIsNotNullButEmailIsNull()
        {
            // Arrange
            var user = new User { Email = null, IsAdmin = false };
            _userSessionMock.Setup(x => x.User).Returns(user);

            // Act
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            Assert.False(authState.User.Identity?.IsAuthenticated ?? false);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAuthenticatedUser_WhenUserIsNotNull()
        {
            // Arrange
            var user = new User { Email = "test@example.com", IsAdmin = false };
            _userSessionMock.Setup(x => x.User).Returns(user);

            // Act
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            Assert.True(authState.User.Identity?.IsAuthenticated);
            Assert.Equal("test@example.com", authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
            Assert.False(authState.User.IsInRole("Admin"));
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAdminUser_WhenUserIsAdmin()
        {
            // Arrange
            var user = new User { Email = "admin@example.com", IsAdmin = true };
            _userSessionMock.Setup(x => x.User).Returns(user);

            // Act
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authState);
            Assert.True(authState.User.Identity?.IsAuthenticated);
            Assert.Equal("admin@example.com", authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
            Assert.True(authState.User.IsInRole("Admin"));
        }

        [Fact]
        public void ShouldNotifyAuthenticationStateChanged_WhenUserSessionChanges()
        {
            // Arrange
            var eventRaised = false;
            _authenticationStateProvider.AuthenticationStateChanged += (task) =>
            {
                eventRaised = true;
            };

            // Act
            _userSessionMock.Raise(m => m.PropertyChanged += null, new PropertyChangedEventArgs(nameof(UserSession.User)));

            // Assert
            Assert.True(eventRaised);
        }
    }
}

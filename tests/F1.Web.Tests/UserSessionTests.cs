
using F1.Web.Models;
using F1.Web.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Services
{
    public class UserSessionTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly UserSession _userSession;

        public UserSessionTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_handlerMock.Object) { BaseAddress = new System.Uri("http://localhost") };
            _userSession = new UserSession(httpClient);
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetUser_WhenApiCallIsSuccessful()
        {
            // Arrange
            var user = new User { Email = "test@example.com", IsAdmin = false };
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(user))
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            await _userSession.InitializeAsync();

            // Assert
            Assert.NotNull(_userSession.User);
            Assert.Equal(user.Email, _userSession.User.Email);
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetUserToNull_WhenApiCallFails()
        {
            // Arrange
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API is down"));

            // Act
            await _userSession.InitializeAsync();

            // Assert
            Assert.Null(_userSession.User);
        }
    }
}

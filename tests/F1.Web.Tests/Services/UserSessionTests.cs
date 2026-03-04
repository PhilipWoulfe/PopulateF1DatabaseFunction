using Bunit;
using F1.Web.Models;
using F1.Web.Services;
using Moq;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Services
{
    public class UserSessionTests : TestContext
    {
        private readonly MockHttpMessageHandler _httpMessageHandler;
        private readonly UserSession _userSession;

        public UserSessionTests()
        {
            _httpMessageHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(_httpMessageHandler)
            {
                BaseAddress = new System.Uri("http://localhost")
            };
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _userSession = new UserSession(httpClientFactory.Object);
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetUser_WhenApiCallIsSuccessful()
        {
            // Arrange
            var user = new User { Email = "test@example.com", IsAdmin = false };
            var json = JsonSerializer.Serialize(user);
            _httpMessageHandler.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };

            // Act
            await _userSession.InitializeAsync();

            // Assert
            Assert.NotNull(_userSession.User);
            Assert.Equal("test@example.com", _userSession.User.Email);
            Assert.False(_userSession.User.IsAdmin);
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetUserToNull_WhenApiCallFails()
        {
            // Arrange
            _httpMessageHandler.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            };

            // Act
            await _userSession.InitializeAsync();

            // Assert
            Assert.Null(_userSession.User);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            public HttpResponseMessage? Response { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Response!);
            }
        }
    }
}

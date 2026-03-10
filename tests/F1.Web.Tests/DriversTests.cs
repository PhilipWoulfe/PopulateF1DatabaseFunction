using F1.Web.Models;
using F1.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Pages
{
    public class DriversTests : TestContext
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;

        public DriversTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            Services.AddSingleton(_httpClient);
        }

        [Fact]
        public void Drivers_ShouldRenderLoading_WhenDataIsBeingFetched()
        {
            // Arrange
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(tcs.Task);

            // Act
            var cut = RenderComponent<Drivers>();

            // Assert
            Assert.Contains("Loading...", cut.Markup);

            // Cleanup
            tcs.SetResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
        }

        [Fact]
        public void Drivers_ShouldRenderTable_WhenApiReturnsData()
        {
            // Arrange
            var mockDrivers = new List<Driver>
            {
                new() { FullName = "Charles Leclerc", DriverId = "leclerc", PermanentNumber = 16, Code = "LEC", Nationality = "Monegasque" },
                new() { FullName = "Carlos Sainz", DriverId = "sainz", PermanentNumber = 55, Code = "SAI", Nationality = "Spanish" }
            };

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockDrivers))
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var cut = RenderComponent<Drivers>();

            // Assert
            cut.WaitForState(() => cut.FindAll("tbody tr").Count > 0);
            var rows = cut.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            Assert.Contains("Charles Leclerc", rows[0].InnerHtml);
            Assert.Contains("Carlos Sainz", rows[1].InnerHtml);
        }

        [Fact]
        public void Drivers_ShouldShowError_WhenApiCallFails()
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
            var cut = RenderComponent<Drivers>();

            // Assert
            cut.WaitForState(() => cut.FindAll("div.alert.alert-danger").Count > 0);
            Assert.Contains("Error fetching drivers: API is down", cut.Markup);
        }
    }
}


using Bunit;
using F1.Web.Pages;
using F1.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace F1.Web.Tests.Pages;

public class ResultsTests : TestContext
{
    [Fact]
    public void Results_ShouldRenderTable_WhenApiReturnsData()
    {
        // Arrange
        var mockResults = new List<RaceResult>
        {
            new() { DriverId = "verstappen", Position = 1, Points = 25 },
            new() { DriverId = "norris", Position = 2, Points = 18 }
        };

        // 1. Mock the HttpMessageHandler to intercept the HTTP request
        var handlerMock = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(mockResults))
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // 2. Mock IHttpClientFactory to return our intercepted client
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient("F1Api")).Returns(httpClient);

        // 3. Register the mock in the bUnit service provider
        Services.AddSingleton(factoryMock.Object);

        // Act
        var cut = RenderComponent<Results>();

        // Assert
        // Wait for the table to appear (handling the async data fetch)
        cut.WaitForState(() => cut.FindAll("tbody tr").Count > 0);

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("verstappen", rows[0].InnerHtml);
        Assert.Contains("norris", rows[1].InnerHtml);
    }
}

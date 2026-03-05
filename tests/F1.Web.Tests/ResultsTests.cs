using F1.Web.Pages;
using F1.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Pages;

public class ResultsTests : TestContext
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;

    public ResultsTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        Services.AddSingleton(_httpClient);
    }

    [Fact]
    public void Results_ShouldRenderLoading_WhenDataIsBeingFetched()
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
        var cut = RenderComponent<Results>();

        // Assert
        Assert.Contains("Loading race results...", cut.Markup);

        // Cleanup
        tcs.SetResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("[]")
        });
    }

    [Fact]
    public void Results_ShouldRenderTable_WhenApiReturnsData()
    {
        // Arrange
        var mockResults = new List<RaceResult>
        {
            new() { DriverId = "verstappen", Position = 1, Points = 25 },
            new() { DriverId = "norris", Position = 2, Points = 18 }
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(mockResults))
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var cut = RenderComponent<Results>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tbody tr").Count > 0);
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("verstappen", rows[0].InnerHtml);
        Assert.Contains("norris", rows[1].InnerHtml);
    }

    [Fact]
    public void Results_ShouldNotBeEmpty_WhenApiReturnsData()
    {
        // Arrange
        var mockResults = new List<RaceResult>
        {
            new() { DriverId = "verstappen", Position = 1, Points = 25 },
            new() { DriverId = "norris", Position = 2, Points = 18 }
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(mockResults))
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var cut = RenderComponent<Results>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tbody tr").Count > 0);
        var rows = cut.FindAll("tbody tr");
        Assert.NotEmpty(rows);
    }
    
    [Fact]
    public void Results_ShouldShowError_WhenApiCallFails()
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
        var cut = RenderComponent<Results>();

        // Assert
        cut.WaitForState(() => cut.FindAll("p").Count > 0);
        Assert.Contains("API is down", cut.Markup);
    }
}

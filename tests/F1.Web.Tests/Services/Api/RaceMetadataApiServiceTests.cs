using F1.Web.Models;
using F1.Web.Services.Api;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Services.Api;

public class RaceMetadataApiServiceTests
{
    [Fact]
    public async Task GetPublishedAsync_WhenResponseIsSuccess_ReturnsMetadata()
    {
        // Arrange
        var metadata = new RaceQuestionMetadata
        {
            RaceId = "2025-24-yas_marina",
            H2HQuestion = "Who finishes higher?",
            BonusQuestion = "How many safety cars?",
            IsPublished = true,
            UpdatedAtUtc = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(metadata), System.Text.Encoding.UTF8, "application/json")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new RaceMetadataApiService(httpClient);

        // Act
        var result = await service.GetPublishedAsync("2025-24-yas_marina");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2025-24-yas_marina", result.RaceId);
        Assert.Equal("Who finishes higher?", result.H2HQuestion);
        Assert.True(result.IsPublished);
    }

    [Fact]
    public async Task GetPublishedAsync_WhenResponseIsNotFound_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new RaceMetadataApiService(httpClient);

        // Act
        var result = await service.GetPublishedAsync("2025-24-yas_marina");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublishedAsync_WhenResponseIsServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Server error", System.Text.Encoding.UTF8, "text/plain")
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new RaceMetadataApiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetPublishedAsync("2025-24-yas_marina"));
    }

    [Fact]
    public async Task GetPublishedAsync_WhenRaceIdIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new RaceMetadataApiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPublishedAsync(""));
    }
}

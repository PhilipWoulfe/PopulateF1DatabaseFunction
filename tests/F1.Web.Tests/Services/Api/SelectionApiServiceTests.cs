using F1.Web.Models;
using F1.Web.Services.Api;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace F1.Web.Tests.Services.Api;

public class SelectionApiServiceTests
{
    [Fact]
    public async Task GetConfigAsync_WhenResponseIsSuccess_ReturnsRaceConfig()
    {
        // Arrange
        var config = new RaceConfig
        {
            RaceId = "2025-24-yas_marina",
            PreQualyDeadlineUtc = new DateTime(2025, 12, 7, 4, 30, 0, DateTimeKind.Utc),
            FinalDeadlineUtc = new DateTime(2025, 12, 8, 3, 30, 0, DateTimeKind.Utc)
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(config), System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        // Act
        var result = await service.GetConfigAsync("2025-24-yas_marina");

        // Assert
        Assert.Equal("2025-24-yas_marina", result.RaceId);
        Assert.Equal(new DateTime(2025, 12, 7, 4, 30, 0, DateTimeKind.Utc), result.PreQualyDeadlineUtc);
    }

    [Fact]
    public async Task GetConfigAsync_WhenResponseBodyIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetConfigAsync("2025-24-yas_marina"));
        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMineAsync_WhenResponseIsSuccess_ReturnsSelection()
    {
        // Arrange
        var selection = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.PreQualy,
            IsLocked = false,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(selection), System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        // Act
        var result = await service.GetMineAsync("2025-24-yas_marina");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(selection.Id, result.Id);
        Assert.Equal("2025-24-yas_marina", result.RaceId);
        Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task GetMineAsync_WhenResponseIsNotFound_ReturnsNull()
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
        var service = new SelectionApiService(httpClient);

        // Act
        var result = await service.GetMineAsync("2025-24-yas_marina");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenResponseIsSuccess_ReturnsCurrentSelections()
    {
        // Arrange
        var current = new[]
        {
            new CurrentSelectionItem
            {
                Position = 1,
                UserId = "user1@example.com",
                UserName = "User 1",
                DriverId = "norris",
                DriverName = "Lando Norris",
                SelectionType = "Regular"
            },
            new CurrentSelectionItem
            {
                Position = 2,
                UserId = "user2@example.com",
                UserName = "User 2",
                DriverId = "leclerc",
                DriverName = "Charles Leclerc",
                SelectionType = "PreQualy"
            }
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(current), System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        // Act
        var result = await service.GetCurrentAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("norris", result[0].DriverId);
        Assert.Equal("leclerc", result[1].DriverId);
    }

    [Fact]
    public async Task SaveMineAsync_WhenSuccessful_ReturnsSavedSelection()
    {
        // Arrange
        var saved = new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.PreQualy,
            IsLocked = false,
            OrderedSelections =
            [
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" }
            ]
        };

        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(saved), System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        var submission = new SelectionSubmission
        {
            BetType = BetType.PreQualy,
            OrderedSelections = [new SelectionPosition { Position = 1, DriverId = "norris" }]
        };

        // Act
        var result = await service.SaveMineAsync("2025-24-yas_marina", submission);

        // Assert
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal(BetType.PreQualy, result.BetType);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Validation failed")]
    [InlineData(HttpStatusCode.Forbidden, "Not authorized")]
    [InlineData(HttpStatusCode.NotFound, "Selection not found")]
    public async Task SaveMineAsync_WhenCommonErrorStatusWithJsonMessage_ThrowsSelectionApiException(HttpStatusCode statusCode, string message)
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(JsonSerializer.Serialize(new { message }), System.Text.Encoding.UTF8, "application/json")
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
        var service = new SelectionApiService(httpClient);

        var submission = new SelectionSubmission { BetType = BetType.Regular };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SelectionApiException>(() => service.SaveMineAsync("2025-24-yas_marina", submission));
        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public async Task SaveMineAsync_WhenErrorStatusWithPlainTextMessage_ThrowsSelectionApiExceptionWithTextMessage()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("Invalid selection data", System.Text.Encoding.UTF8, "text/plain")
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
        var service = new SelectionApiService(httpClient);

        var submission = new SelectionSubmission { BetType = BetType.Regular };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SelectionApiException>(() => service.SaveMineAsync("2025-24-yas_marina", submission));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("Invalid selection data", ex.Message);
    }

    [Fact]
    public async Task SaveMineAsync_WhenRaceIdIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new SelectionApiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMineAsync("", new SelectionSubmission()));
    }

    [Fact]
    public async Task SaveMineAsync_WhenSubmissionIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new SelectionApiService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveMineAsync("2025-24-yas_marina", null!));
    }
}

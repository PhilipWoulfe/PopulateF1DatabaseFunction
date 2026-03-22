using System.Net;
using System.Text;
using System.Text.Json;
using F1.Web.Services;
using F1.Web.Services.Api;
using Moq;
using Moq.Protected;

namespace F1.Web.Tests.Services;

public class MockDateServiceTests
{
    private static HttpClient BuildHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
    }

    private static HttpResponseMessage JsonResponse(object payload, HttpStatusCode status = HttpStatusCode.OK)
        => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    // ─── GetMockDate ──────────────────────────────────────────────────────────

    [Fact]
    public void GetMockDate_BeforeRefresh_ReturnsNull()
    {
        var service = new MockDateService(BuildHttpClient(JsonResponse(new { mockDate = (DateTime?)null })));

        Assert.Null(service.GetMockDate());
    }

    // ─── RefreshAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_WhenApiReturnsMockDate_SetsMockDate()
    {
        var expected = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);
        var service = new MockDateService(BuildHttpClient(JsonResponse(new { mockDate = expected })));

        await service.RefreshAsync();

        Assert.Equal(expected, service.GetMockDate());
    }

    [Fact]
    public async Task RefreshAsync_WhenApiReturnsNullMockDate_SetsNull()
    {
        var service = new MockDateService(BuildHttpClient(JsonResponse(new { mockDate = (DateTime?)null })));

        await service.RefreshAsync();

        Assert.Null(service.GetMockDate());
    }

    [Fact]
    public async Task RefreshAsync_WhenApiReturnsNonSuccess_SetsNullAndDoesNotThrow()
    {
        var service = new MockDateService(BuildHttpClient(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // RefreshAsync is fire-and-forget; failures must not surface to pages
        var ex = await Record.ExceptionAsync(() => service.RefreshAsync());

        Assert.Null(ex);
        Assert.Null(service.GetMockDate());
    }

    [Fact]
    public async Task RefreshAsync_WhenHttpRequestExceptionThrown_SetsNullAndDoesNotThrow()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new MockDateService(httpClient);

        var ex = await Record.ExceptionAsync(() => service.RefreshAsync());

        Assert.Null(ex);
        Assert.Null(service.GetMockDate());
    }

    [Fact]
    public async Task RefreshAsync_WhenResponseBodyIsMalformedJson_SetsNullAndDoesNotThrow()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "application/json")
        };
        var service = new MockDateService(BuildHttpClient(response));

        var ex = await Record.ExceptionAsync(() => service.RefreshAsync());

        Assert.Null(ex);
        Assert.Null(service.GetMockDate());
    }

    [Fact]
    public async Task RefreshAsync_AfterPreviousSuccess_ClearsDateOnFailure()
    {
        var expected = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);

        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? JsonResponse(new { mockDate = expected })
                    : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new MockDateService(httpClient);

        await service.RefreshAsync();
        Assert.Equal(expected, service.GetMockDate());

        await service.RefreshAsync();
        Assert.Null(service.GetMockDate());
    }

    // ─── SetMockDateAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetMockDateAsync_WhenSuccess_SetsMockDate()
    {
        var dateUtc = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);
        var service = new MockDateService(BuildHttpClient(new HttpResponseMessage(HttpStatusCode.NoContent)));

        await service.SetMockDateAsync(dateUtc);

        Assert.Equal(dateUtc, service.GetMockDate());
    }

    [Fact]
    public async Task SetMockDateAsync_WithNull_ClearsMockDate()
    {
        // First set a date so we can verify it gets cleared
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.NoContent));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new MockDateService(httpClient);

        await service.SetMockDateAsync(new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc));
        await service.SetMockDateAsync(null);

        Assert.Null(service.GetMockDate());
    }

    [Fact]
    public async Task SetMockDateAsync_NormalizesDateKindToUtc()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        HttpRequestMessage? capturedRequest = null;
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new MockDateService(httpClient);

        var unspecified = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Unspecified);
        await service.SetMockDateAsync(unspecified);

        // Local state stored as UTC, preserving the original instant (no shift)
        var stored = service.GetMockDate();
        Assert.NotNull(stored);
        Assert.Equal(DateTimeKind.Utc, stored!.Value.Kind);
        Assert.Equal(unspecified.Ticks, stored.Value.Ticks);
        Assert.Equal(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc), stored.Value);

        // Outgoing request body should contain mockDateUtc as a UTC value
        Assert.NotNull(capturedRequest);
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("2025-12-07T10:00:00", body);
    }

    [Fact]
    public async Task SetMockDateAsync_WhenApiForbidden_ThrowsApiServiceException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { message = "Admin role required." }),
                Encoding.UTF8, "application/json")
        };
        var service = new MockDateService(BuildHttpClient(response));

        var ex = await Assert.ThrowsAsync<ApiServiceException>(() => service.SetMockDateAsync(DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.Forbidden, ex.Error.StatusCode);
        Assert.Equal("Admin role required.", ex.Error.Message);
    }

    [Fact]
    public async Task SetMockDateAsync_WhenServerError_ThrowsApiServiceException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error", Encoding.UTF8, "text/plain")
        };
        var service = new MockDateService(BuildHttpClient(response));

        var ex = await Assert.ThrowsAsync<ApiServiceException>(() => service.SetMockDateAsync(DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.Error.StatusCode);
    }

    [Fact]
    public async Task SetMockDateAsync_WhenApiReturnsError_DoesNotUpdateLocalState()
    {
        var initialDate = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);

        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.NoContent)
                    : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        var service = new MockDateService(httpClient);

        await service.SetMockDateAsync(initialDate);
        Assert.Equal(initialDate, service.GetMockDate());

        await Assert.ThrowsAsync<ApiServiceException>(() => service.SetMockDateAsync(DateTime.UtcNow.AddDays(1)));

        // Original date should be unchanged since the second call failed
        Assert.Equal(initialDate, service.GetMockDate());
    }
}

using F1.Web.Models;
using F1.Web.Services.Api;
using System.Net;
using System.Text;
using System.Text.Json;

namespace F1.Web.Tests.Services.Api;

public class SelectionApiServiceTests
{
    [Fact]
    public async Task SaveMineAsync_WhenSuccessful_ReturnsSavedSelectionFromResponseBody()
    {
        var handler = new QueueHttpMessageHandler();
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
            ],
            Selections = ["norris", "leclerc"]
        };

        handler.EnqueueResponse(CreateJsonResponse(saved, HttpStatusCode.OK));

        var service = CreateService(handler);
        var submission = new SelectionSubmission
        {
            BetType = BetType.PreQualy,
            OrderedSelections =
            [
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" }
            ],
            Selections = ["norris", "leclerc"]
        };

        var result = await service.SaveMineAsync("2025-24-yas_marina", submission);

        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("2025-24-yas_marina", result.RaceId);
        Assert.Equal(BetType.PreQualy, result.BetType);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task SaveMineAsync_WhenCommonErrorStatusWithJsonMessage_ThrowsSelectionApiExceptionWithMessage(HttpStatusCode statusCode)
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new { message = "API-provided error message." }, statusCode));

        var service = CreateService(handler);
        var submission = new SelectionSubmission
        {
            BetType = BetType.Regular,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        var ex = await Assert.ThrowsAsync<SelectionApiException>(() => service.SaveMineAsync("2025-24-yas_marina", submission));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal("API-provided error message.", ex.Message);
    }

    [Fact]
    public async Task SaveMineAsync_WhenCommonErrorStatusWithTextBody_ThrowsSelectionApiExceptionWithTextMessage()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Plain text failure", Encoding.UTF8, "text/plain")
        });

        var service = CreateService(handler);
        var submission = new SelectionSubmission
        {
            BetType = BetType.Regular,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        var ex = await Assert.ThrowsAsync<SelectionApiException>(() => service.SaveMineAsync("2025-24-yas_marina", submission));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("Plain text failure", ex.Message);
    }

    private static SelectionApiService CreateService(QueueHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        return new SelectionApiService(httpClient);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No queued HTTP response for request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}

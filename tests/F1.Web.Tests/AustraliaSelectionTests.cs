using System.Net;
using System.Text;
using System.Text.Json;
using F1.Web.Models;
using F1.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace F1.Web.Tests.Pages;

public class AustraliaSelectionTests : TestContext
{
    private static readonly RaceConfig DefaultRaceConfig = new()
    {
        RaceId = "2026-australia",
        PreQualyDeadlineUtc = new DateTime(2026, 3, 7, 4, 30, 0, DateTimeKind.Utc),
        FinalDeadlineUtc = new DateTime(2026, 3, 8, 3, 30, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void AustraliaSelection_ShouldRenderWarningAndCountdown_WhenLoadedWithNoExistingSelection()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = RenderComponent<AustraliaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("Locking for Pre-Qualy gives +50% points", cut.Markup));
        Assert.Contains("Countdown:", cut.Markup);
    }

    [Fact]
    public void AustraliaSelection_ShouldRenderLockedState_WhenExistingSelectionIsLocked()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(CreateJsonResponse(new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2026-australia",
            UserId = "user@example.com",
            BetType = BetType.PreQualy,
            IsLocked = true,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        }));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = RenderComponent<AustraliaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("This pre-qualy selection is locked.", cut.Markup));
        Assert.True(cut.Find("button[type='submit']").HasAttribute("disabled"));
    }

    [Fact]
    public void AustraliaSelection_ShouldSaveSelection_WhenSubmitSucceeds()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2026-australia",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            IsLocked = false,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        }));
        handler.EnqueueResponse(CreateJsonResponse(new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2026-australia",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            IsLocked = false,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        }));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = RenderComponent<AustraliaSelection>();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("select").Count));

        ChangeSelect(cut, 0, "norris");
        ChangeSelect(cut, 1, "leclerc");
        ChangeSelect(cut, 2, "hamilton");
        ChangeSelect(cut, 3, "piastri");
        ChangeSelect(cut, 4, "verstappen");

        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Selection saved successfully.", cut.Markup));
        Assert.Contains(handler.Requests, req => req.Method == HttpMethod.Put && req.RequestUri?.AbsolutePath.EndsWith("/selections/2026-australia/mine") == true);
    }

    [Fact]
    public void AustraliaSelection_ShouldShowApiErrorMessage_WhenSaveFails()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(new { message = "Exactly 5 unique drivers must be selected." }, HttpStatusCode.BadRequest));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = RenderComponent<AustraliaSelection>();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("select").Count));

        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Exactly 5 unique drivers must be selected.", cut.Markup));
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static void ChangeSelect(IRenderedComponent<AustraliaSelection> cut, int index, string value)
    {
        cut.FindAll("select")[index].Change(value);
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<HttpRequestMessage> Requests { get; } = [];

        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No queued HTTP response for request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}

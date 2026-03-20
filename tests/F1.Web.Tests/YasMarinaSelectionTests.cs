using F1.Web.Models;
using F1.Web.Pages;
using F1.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace F1.Web.Tests.Pages;

public class YasMarinaSelectionTests : BunitContext
{
    public YasMarinaSelectionTests()
    {
        // Register a default ITimeProvider for all tests
        Services.AddSingleton<ITimeProvider, DefaultTimeProvider>();
        Services.AddSingleton<IMockDateService, TestMockDateService>();
    }

    private static readonly RaceConfig DefaultRaceConfig = new()
    {
        RaceId = "2025-24-yas_marina",
        PreQualyDeadlineUtc = new DateTime(2025, 12, 7, 4, 30, 0, DateTimeKind.Utc),
        FinalDeadlineUtc = new DateTime(2025, 12, 8, 3, 30, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void YasMarinaSelection_ShouldRenderWarningAndCountdown_WhenLoadedWithNoExistingSelection()
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
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("Locking for Pre-Qualy gives +50% points", cut.Markup));
        Assert.Contains("Countdown:", cut.Markup);
        Assert.Equal(string.Empty, cut.FindAll("select")[0].GetAttribute("value"));
    }

    [Fact]
    public void YasMarinaSelection_ShouldRenderPublishedRaceQuestions_WhenMetadataExists()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(CreateJsonResponse(new RaceQuestionMetadata
        {
            RaceId = "2025-24-yas_marina",
            H2HQuestion = "Who finishes higher: Leclerc or Norris?",
            BonusQuestion = "How many safety-car laps?",
            IsPublished = true,
            UpdatedAtUtc = DateTime.UtcNow
        }));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("Race Questions", cut.Markup));
        Assert.Contains("Who finishes higher: Leclerc or Norris?", cut.Markup);
        Assert.Contains("How many safety-car laps?", cut.Markup);
    }

    [Fact]
    public void YasMarinaSelection_ShouldRenderLockedState_WhenExistingSelectionIsLocked()
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
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            IsLocked = true,
            Selections = ["leclerc", "norris", "hamilton", "piastri", "verstappen"]
        }));
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new CurrentSelectionItem
            {
                Position = 1,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "norris",
                DriverName = "Lando Norris",
                SelectionType = "PreQualy",
                Timestamp = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc)
            },
            new CurrentSelectionItem
            {
                Position = 2,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "leclerc",
                DriverName = "Charles Leclerc",
                SelectionType = "PreQualy",
                Timestamp = new DateTime(2025, 12, 6, 10, 1, 0, DateTimeKind.Utc)
            }
        }));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("This pre-qualy selection is locked.", cut.Markup));
        Assert.True(cut.Find("button[type='submit']").HasAttribute("disabled"));
        Assert.Equal("norris", cut.FindAll("select")[0].GetAttribute("value"));
        Assert.Equal("leclerc", cut.FindAll("select")[1].GetAttribute("value"));
        Assert.True(cut.Find("#strategy-prequaly").HasAttribute("checked"));
    }

    [Fact]
    public void YasMarinaSelection_ShouldSaveSelection_WhenSubmitSucceeds()
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
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));
        handler.EnqueueResponse(CreateJsonResponse(new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            IsLocked = false,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        }));
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new CurrentSelectionItem
            {
                Position = 1,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "norris",
                DriverName = "Lando Norris",
                SelectionType = "Regular",
                Timestamp = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc)
            },
            new CurrentSelectionItem
            {
                Position = 2,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "leclerc",
                DriverName = "Charles Leclerc",
                SelectionType = "Regular",
                Timestamp = new DateTime(2025, 12, 6, 10, 1, 0, DateTimeKind.Utc)
            }
        }));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        handler.EnqueueResponse(CreateJsonResponse(new Selection
        {
            Id = Guid.NewGuid(),
            RaceId = "2025-24-yas_marina",
            UserId = "user@example.com",
            BetType = BetType.Regular,
            IsLocked = false,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        }));
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new CurrentSelectionItem
            {
                Position = 1,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "norris",
                DriverName = "Lando Norris",
                SelectionType = "Regular",
                Timestamp = new DateTime(2025, 12, 6, 11, 0, 0, DateTimeKind.Utc)
            }
        }));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("select").Count));

        ChangeSelect(cut, 0, "norris");
        ChangeSelect(cut, 1, "leclerc");
        ChangeSelect(cut, 2, "hamilton");
        ChangeSelect(cut, 3, "piastri");
        ChangeSelect(cut, 4, "verstappen");

        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Selection saved successfully.", cut.Markup));
        Assert.Contains(handler.Requests, req => req.Method == HttpMethod.Put && req.RequestUri?.AbsolutePath.EndsWith("/selections/2025-24-yas_marina/mine") == true);
        Assert.Contains(handler.Requests, req => req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath.EndsWith("/selections/current") == true);
    }

    [Fact]
    public void YasMarinaSelection_ShouldShowApiErrorMessage_WhenSaveFails()
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
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));
        handler.EnqueueResponse(CreateJsonResponse(new { message = "Exactly 5 unique drivers must be selected." }, HttpStatusCode.BadRequest));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();
        cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("select").Count));

        cut.Find("button[type='submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Exactly 5 unique drivers must be selected.", cut.Markup));
    }

    [Fact]
    public async Task YasMarinaSelection_DisposeAsync_ShouldBeIdempotent()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();
        cut.WaitForAssertion(() => Assert.Contains("Countdown:", cut.Markup));

        var component = (IAsyncDisposable)cut.Instance;
        await component.DisposeAsync();
        // Second call must not throw
        await component.DisposeAsync();
    }

    [Fact]
    public void YasMarinaSelection_ShouldPopulateControls_FromCurrentSelectionsSnapshot()
    {
        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(DefaultRaceConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new CurrentSelectionItem
            {
                Position = 1,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "norris",
                DriverName = "Lando Norris",
                SelectionType = "PreQualy",
                Timestamp = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc)
            },
            new CurrentSelectionItem
            {
                Position = 2,
                UserId = "user@example.com",
                UserName = "user@example.com",
                DriverId = "leclerc",
                DriverName = "Charles Leclerc",
                SelectionType = "PreQualy",
                Timestamp = new DateTime(2025, 12, 6, 10, 1, 0, DateTimeKind.Utc)
            }
        }));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();

        cut.WaitForAssertion(() => Assert.Equal("norris", cut.FindAll("select")[0].GetAttribute("value")));
        Assert.Equal("leclerc", cut.FindAll("select")[1].GetAttribute("value"));
        Assert.True(cut.Find("#strategy-prequaly").HasAttribute("checked"));
    }

    [Fact]
    public void YasMarinaSelection_ShouldRenderLockedState_WhenPastFinalDeadline()
    {
        var finalDeadline = new DateTime(2025, 12, 8, 12, 0, 0, DateTimeKind.Utc);
        var pastDeadlineConfig = new RaceConfig
        {
            RaceId = "2025-24-yas_marina",
            PreQualyDeadlineUtc = new DateTime(2025, 12, 7, 13, 0, 0, DateTimeKind.Utc),
            FinalDeadlineUtc = finalDeadline
        };

        Services.AddSingleton<ITimeProvider>(new FrozenTimeProvider(new DateTime(2025, 12, 8, 12, 1, 0, DateTimeKind.Utc)));

        var handler = new QueueHttpMessageHandler();
        handler.EnqueueResponse(CreateJsonResponse(new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        }));
        handler.EnqueueResponse(CreateJsonResponse(pastDeadlineConfig));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        handler.EnqueueResponse(CreateJsonResponse(Array.Empty<CurrentSelectionItem>()));

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var cut = Render<YasMarinaSelection>();

        cut.WaitForAssertion(() => Assert.Contains("All deadlines have passed.", cut.Markup));
        Assert.True(cut.Find("button[type='submit']").HasAttribute("disabled"));
        Assert.True(cut.FindAll("select").All(s => s.HasAttribute("disabled")));
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static void ChangeSelect(IRenderedComponent<YasMarinaSelection> cut, int index, string value)
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

    private sealed class FrozenTimeProvider(DateTime frozenUtc) : ITimeProvider
    {
        public DateTime UtcNow => frozenUtc;
    }

    private sealed class TestMockDateService : IMockDateService
    {
        public DateTime? GetMockDate()
        {
            return null;
        }

        public Task RefreshAsync()
        {
            return Task.CompletedTask;
        }

        public Task SetMockDateAsync(DateTime? dateUtc)
        {
            return Task.CompletedTask;
        }
    }
}

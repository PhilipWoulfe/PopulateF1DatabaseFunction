using F1.Api.Controllers;
using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;

namespace F1.Api.Tests.Controllers;

public class SelectionsControllerTests
{
    [Fact]
    public async Task GetCurrent_ShouldReturnUnauthorized_WhenUserCannotBeResolved()
    {
        var serviceMock = new Mock<ISelectionService>();
        var controller = CreateController(serviceMock);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetCurrent();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetCurrent_ShouldReturnOk_WithSelectionRows()
    {
        var serviceMock = new Mock<ISelectionService>();
        serviceMock
            .Setup(service => service.GetCurrentSelectionsAsync("user@example.com"))
            .ReturnsAsync([
                new CurrentSelectionDto
                {
                    Position = 1,
                    UserId = "user@example.com",
                    UserName = "user@example.com",
                    DriverId = "norris",
                    DriverName = "Lando Norris",
                    SelectionType = "Regular",
                    Timestamp = new DateTime(2026, 3, 6, 9, 0, 0, DateTimeKind.Utc)
                }
            ]);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "user@example.com")],
            "TestAuth"));

        var controller = CreateController(serviceMock);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.GetCurrent();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<CurrentSelectionDto>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(1, payload[0].Position);
        Assert.Equal("norris", payload[0].DriverId);
    }

    [Fact]
    public async Task GetCurrent_ShouldReturnMockRows_InDevelopmentWhenEnabled()
    {
        const string userId = "mock-current@example.com";
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, userId)],
            "TestAuth"));

        var service = BuildSelectionServiceWithMockCurrentSelections();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);
        var controller = new SelectionsController(service, dateTimeProvider.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Upsert a mock selection for the same user as in the context
        await controller.UpsertMine("2026-australia", new SelectionSubmissionDto
        {
            BetType = F1.Core.Models.BetType.Regular,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "max_verstappen" },
                new SelectionPosition { Position = 2, DriverId = "lando_norris" },
                new SelectionPosition { Position = 3, DriverId = "charles_leclerc" },
                new SelectionPosition { Position = 4, DriverId = "oscar_piastri" },
                new SelectionPosition { Position = 5, DriverId = "lewis_hamilton" }
            }
        });

        var result = await controller.GetCurrent();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<CurrentSelectionDto>>(ok.Value);
        Assert.NotEmpty(payload);
        Assert.Equal(1, payload[0].Position);
        Assert.Equal("max_verstappen", payload[0].DriverId);
    }

    [Fact]
    public async Task GetMine_ShouldReturnPersistedMockSelection_InDevelopmentWhenEnabled()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "user@example.com")],
            "TestAuth"));

        var service = BuildSelectionServiceWithMockCurrentSelections();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);
        var controller = new SelectionsController(service, dateTimeProvider.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        await controller.UpsertMine("2026-australia", new SelectionSubmissionDto
        {
            BetType = F1.Core.Models.BetType.PreQualy,
            OrderedSelections = new List<SelectionPosition>
            {
                new SelectionPosition { Position = 1, DriverId = "norris" },
                new SelectionPosition { Position = 2, DriverId = "leclerc" },
                new SelectionPosition { Position = 3, DriverId = "hamilton" },
                new SelectionPosition { Position = 4, DriverId = "piastri" },
                new SelectionPosition { Position = 5, DriverId = "verstappen" }
            }
        });

        var result = await controller.GetMine("2026-australia");

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<F1.Core.Models.Selection>(ok.Value);
        Assert.Equal(F1.Core.Models.BetType.PreQualy, payload.BetType);
        Assert.Equal("norris", payload.OrderedSelections[0].DriverId);
    }

    public ISelectionService BuildSelectionServiceWithMockCurrentSelections()
    {
        var mockRepo = new Mock<ISelectionRepository>();
        var mockDriverRepo = new Mock<IDriverRepository>();
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();

        // Mock driver repository to return drivers used in the test selections
        mockDriverRepo.Setup(repo => repo.GetDriversAsync()).ReturnsAsync(new List<Driver>
        {
            new Driver { DriverId = "max_verstappen", FullName = "Max Verstappen" },
            new Driver { DriverId = "lando_norris", FullName = "Lando Norris" },
            new Driver { DriverId = "charles_leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "oscar_piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "lewis_hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" },
            new Driver { DriverId = "hamilton", FullName = "Lewis Hamilton" },
            new Driver { DriverId = "piastri", FullName = "Oscar Piastri" },
            new Driver { DriverId = "verstappen", FullName = "Max Verstappen" }
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DevSettings:MockCurrentSelections", "true" }
            })
            .Build();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(env => env.EnvironmentName).Returns(Environments.Development);

        return new SelectionService(mockRepo.Object, mockDriverRepo.Object, mockDateTimeProvider.Object, configuration, hostEnvironment.Object);
    }

    private static SelectionsController CreateController(
        Mock<ISelectionService> serviceMock,
        bool mockCurrentSelections = false,
        string environmentName = "Production")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DevSettings:MockCurrentSelections", mockCurrentSelections.ToString() }
            })
            .Build();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(env => env.EnvironmentName).Returns(environmentName);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        return new SelectionsController(serviceMock.Object, dateTimeProvider.Object);
    }
}

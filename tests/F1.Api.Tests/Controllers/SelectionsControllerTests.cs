using F1.Api.Controllers;
using F1.Core.Dtos;
using F1.Core.Interfaces;
using F1.Core.Models;
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
        var serviceMock = new Mock<ISelectionService>();
        const string userId = "mock-current@example.com";

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, userId)],
            "TestAuth"));

        var controller = CreateController(serviceMock, mockCurrentSelections: true, environmentName: Environments.Development);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.GetCurrent();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<CurrentSelectionDto>>(ok.Value);
        Assert.NotEmpty(payload);
        Assert.Equal(1, payload[0].Position);
        Assert.Equal("max_verstappen", payload[0].DriverId);

        serviceMock.Verify(service => service.GetCurrentSelectionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMine_ShouldReturnPersistedMockSelection_InDevelopmentWhenEnabled()
    {
        var serviceMock = new Mock<ISelectionService>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "user@example.com")],
            "TestAuth"));

        var controller = CreateController(serviceMock, mockCurrentSelections: true, environmentName: Environments.Development);
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

        return new SelectionsController(serviceMock.Object, configuration, hostEnvironment.Object);
    }
}

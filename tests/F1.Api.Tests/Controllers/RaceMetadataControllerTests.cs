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

public class RaceMetadataControllerTests
{
    [Fact]
    public async Task GetMetadata_ShouldReturnNotFound_WhenNoPublishedMetadataExists()
    {
        var serviceMock = new Mock<IRaceMetadataService>();
        serviceMock.Setup(service => service.GetMetadataAsync("2025-24-yas_marina", true)).ReturnsAsync((RaceQuestionMetadata?)null);

        var controller = CreateController(serviceMock);

        var result = await controller.GetMetadata("2025-24-yas_marina");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMetadata_ShouldReturnForbid_WhenIncludeDraftRequestedByNonAdmin()
    {
        var serviceMock = new Mock<IRaceMetadataService>();
        var controller = CreateController(serviceMock);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "user@example.com")], "TestAuth"))
            }
        };

        var result = await controller.GetMetadata("2025-24-yas_marina", includeDraft: true);

        Assert.IsType<ForbidResult>(result);
    }

    private static RaceMetadataController CreateController(Mock<IRaceMetadataService> serviceMock)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DevSettings:MockRaceMetadata", "false" }
            })
            .Build();

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(env => env.EnvironmentName).Returns(Environments.Production);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);

        return new RaceMetadataController(serviceMock.Object, configuration, hostEnvironment.Object, dateTimeProvider.Object);
    }

    private static HttpContext BuildAdminContext()
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "admin@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            ], "TestAuth"))
        };
    }
}

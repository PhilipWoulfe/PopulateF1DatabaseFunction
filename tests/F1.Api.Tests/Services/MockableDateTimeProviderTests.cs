
using System;
using F1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class MockableDateTimeProviderTests
{
    [Fact]
    public void UtcNow_Returns_Header_If_Present_And_Valid()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Mock-Date"] = "2025-12-19T12:34:56Z";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        var config = new Mock<IConfiguration>();
        var globalMock = new Mock<IGlobalMockDateService>();
        var provider = new MockableDateTimeProvider(accessor.Object, config.Object, globalMock.Object);

        // Act
        var result = provider.UtcNow;

        // Assert
        Assert.Equal(new DateTime(2025, 12, 19, 12, 34, 56, DateTimeKind.Utc), result);
    }

    [Fact]
    public void UtcNow_Returns_GlobalMockDate_If_Header_Not_Present()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var config = new Mock<IConfiguration>();
        var globalMock = new Mock<IGlobalMockDateService>();
        globalMock.Setup(g => g.GetMockDateUtc()).Returns(new DateTime(2025, 12, 20, 1, 2, 3, DateTimeKind.Utc));
        var provider = new MockableDateTimeProvider(accessor.Object, config.Object, globalMock.Object);

        // Act
        var result = provider.UtcNow;

        // Assert
        Assert.Equal(new DateTime(2025, 12, 20, 1, 2, 3, DateTimeKind.Utc), result);
    }

    [Fact]
    public void UtcNow_Falls_Back_To_SystemTime()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var config = new Mock<IConfiguration>();
        var globalMock = new Mock<IGlobalMockDateService>();
        globalMock.Setup(g => g.GetMockDateUtc()).Returns((DateTime?)null);
        var provider = new MockableDateTimeProvider(accessor.Object, config.Object, globalMock.Object);

        // Act
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = provider.UtcNow;
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.InRange(result, before, after);
    }
}

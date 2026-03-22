using F1.Web.Services;
using Moq;

namespace F1.Web.Tests.Services;

public class MockableTimeProviderTests
{
    [Fact]
    public void UtcNow_WhenMockDateIsSet_ReturnsMockDate()
    {
        var expected = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);
        var mockDateService = new Mock<IMockDateService>();
        mockDateService.Setup(s => s.GetMockDate()).Returns(expected);

        var provider = new MockableTimeProvider(mockDateService.Object);

        Assert.Equal(expected, provider.UtcNow);
    }

    [Fact]
    public void UtcNow_WhenNoMockDate_FallsBackToSystemTime()
    {
        var mockDateService = new Mock<IMockDateService>();
        mockDateService.Setup(s => s.GetMockDate()).Returns((DateTime?)null);

        var provider = new MockableTimeProvider(mockDateService.Object);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = provider.UtcNow;
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(result, before, after);
    }

    [Fact]
    public void UtcNow_DelegatesToMockDateServiceEachCall()
    {
        var first = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2025, 12, 8, 12, 0, 0, DateTimeKind.Utc);

        var mockDateService = new Mock<IMockDateService>();
        var callCount = 0;
        mockDateService
            .Setup(s => s.GetMockDate())
            .Returns(() => callCount++ == 0 ? first : second);

        var provider = new MockableTimeProvider(mockDateService.Object);

        Assert.Equal(first, provider.UtcNow);
        Assert.Equal(second, provider.UtcNow);
    }
}

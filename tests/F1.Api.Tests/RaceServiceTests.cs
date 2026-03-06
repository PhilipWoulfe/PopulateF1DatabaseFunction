using F1.Services;

namespace F1.Api.Tests;

public class RaceServiceTests
{
    [Fact]
    public void GetMockResults_ShouldReturnPopulatedList()
    {
        // Arrange
        // (Note: This will show a red squiggly until we create the servi   ce!)
        var service = new RaceService();

        // Act
        var results = service.GetMockResults();

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.DriverId == "norris2");
    }
}

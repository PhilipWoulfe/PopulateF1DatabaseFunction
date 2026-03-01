using F1.Services;
using F1.Core.Models;
using Xunit;

namespace F1.Tests.Unit;

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
        Assert.Contains(results, r => r.DriverId == "norris");
        Assert.True(false);
    }
}
using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Infrastructure.Tests.Contracts;

public abstract class DriverRepositoryContractTests
{
    protected abstract IDriverRepository CreateRepositoryWithDrivers(IEnumerable<Driver> drivers);

    [Fact]
    public async Task GetDriversAsync_ReturnsAllDrivers()
    {
        var drivers = new List<Driver>
        {
            new() { Id = "1", DriverId = "norris", FullName = "Lando Norris" },
            new() { Id = "2", DriverId = "leclerc", FullName = "Charles Leclerc" }
        };
        var repository = CreateRepositoryWithDrivers(drivers);

        var result = await repository.GetDriversAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetDriversAsync_ReturnsEmptyList_WhenNoDrivers()
    {
        var repository = CreateRepositoryWithDrivers([]);

        var result = await repository.GetDriversAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

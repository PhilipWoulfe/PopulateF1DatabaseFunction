using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Tests.Relational;

public class EfDriverRepositoryContractTests : DriverRepositoryContractTests
{
    protected override IDriverRepository CreateRepositoryWithDrivers(IEnumerable<Driver> drivers)
    {
        var context = CreateContext();
        context.Drivers.AddRange(drivers);
        context.SaveChanges();

        return new EfDriverRepository(context);
    }

    private static F1DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<F1DbContext>()
            .UseInMemoryDatabase($"driver-contract-{Guid.NewGuid():N}")
            .Options;

        return new F1DbContext(options);
    }
}

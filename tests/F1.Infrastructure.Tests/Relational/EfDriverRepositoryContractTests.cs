using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Data;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.EntityFrameworkCore;

namespace F1.Infrastructure.Tests.Relational;

[Collection(PostgresContractCollection.Name)]
public class EfDriverRepositoryContractTests : DriverRepositoryContractTests
{
    private readonly PostgresTestContainerFixture _fixture;

    public EfDriverRepositoryContractTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IDriverRepository CreateRepositoryWithDrivers(IEnumerable<Driver> drivers)
    {
        var context = CreateContext();
        context.Drivers.AddRange(drivers);
        context.SaveChanges();

        return new EfDriverRepository(context);
    }

    private F1DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<F1DbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        var context = new F1DbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        return context;
    }
}

using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Infrastructure.Tests.Cosmos;

public class CosmosDriverRepositoryContractTests : DriverRepositoryContractTests
{
    protected override IDriverRepository CreateRepositoryWithDrivers(IEnumerable<Driver> drivers)
    {
        var driverList = drivers.ToList();

        var feedResponse = new Mock<FeedResponse<Driver>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(driverList.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<Driver>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedResponse.Object);

        var mockContainer = new Mock<Container>();
        mockContainer
            .Setup(c => c.GetItemQueryIterator<Driver>(It.IsAny<QueryDefinition>(), null, null))
            .Returns(feedIterator.Object);

        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(c => c.GetContainer("F12025", "Drivers")).Returns(mockContainer.Object);

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");

        return new CosmosDriverRepository(mockClient.Object, config.Object);
    }
}

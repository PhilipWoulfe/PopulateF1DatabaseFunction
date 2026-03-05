using F1.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using F1.Core.Models;

namespace F1.Api.Tests.Integration
{
    public class CosmosDriverRepositoryTests
    {
        [Fact]
        public async Task GetDriversAsync_ShouldReturnListOfDrivers()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");

            var mockCosmosClient = new Mock<CosmosClient>();
            var mockContainer = new Mock<Container>();
            var mockFeedResponse = new Mock<FeedResponse<Driver>>();

            var expectedDrivers = new List<Driver>
            {
                new Driver { Id = "1", FullName = "Driver 1" },
                new Driver { Id = "2", FullName = "Driver 2" }
            };

            mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(expectedDrivers.GetEnumerator());
            mockFeedResponse.Setup(x => x.Count).Returns(expectedDrivers.Count);
            
            var mockFeedIterator = new Mock<FeedIterator<Driver>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(mockFeedResponse.Object);

            mockContainer.Setup(c => c.GetItemQueryIterator<Driver>(It.IsAny<QueryDefinition>(), null, null))
                         .Returns(mockFeedIterator.Object);

            mockCosmosClient.Setup(c => c.GetContainer("F12025", "Drivers")).Returns(mockContainer.Object);

            var repository = new CosmosDriverRepository(mockCosmosClient.Object, mockConfiguration.Object);

            // Act
            var result = await repository.GetDriversAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDrivers.Count, result.Count);
        }
        [Fact]
        public async Task GetDriversAsync_ShouldReturnEmptyList_WhenNoDriversFound()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");

            var mockCosmosClient = new Mock<CosmosClient>();
            var mockContainer = new Mock<Container>();
            var mockFeedResponse = new Mock<FeedResponse<Driver>>();

            var expectedDrivers = new List<Driver>();

            mockFeedResponse.Setup(x => x.GetEnumerator()).Returns(expectedDrivers.GetEnumerator());
            mockFeedResponse.Setup(x => x.Count).Returns(expectedDrivers.Count);

            var mockFeedIterator = new Mock<FeedIterator<Driver>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(mockFeedResponse.Object);

            mockContainer.Setup(c => c.GetItemQueryIterator<Driver>(It.IsAny<QueryDefinition>(), null, null))
                         .Returns(mockFeedIterator.Object);

            mockCosmosClient.Setup(c => c.GetContainer("F12025", "Drivers")).Returns(mockContainer.Object);

            var repository = new CosmosDriverRepository(mockCosmosClient.Object, mockConfiguration.Object);

            // Act
            var result = await repository.GetDriversAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

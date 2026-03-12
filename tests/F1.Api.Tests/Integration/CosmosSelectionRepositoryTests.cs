using F1.Core.Models;
using F1.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Api.Tests.Integration;

public class CosmosSelectionRepositoryTests
{
    [Fact]
    public async Task UpsertSelectionAsync_ShouldReuseExistingId_ForSameRaceAndUser()
    {
        var existingId = Guid.NewGuid();
        var raceId = "2026-australia";
        var userId = "user@example.com";

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();

        var existingSelection = new Selection
        {
            Id = existingId,
            RaceId = raceId,
            UserId = userId,
            BetType = BetType.Regular,
            Selections = ["norris", "leclerc", "hamilton", "piastri", "verstappen"]
        };

        var feedResponse = new Mock<FeedResponse<Selection>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<Selection> { existingSelection }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<Selection>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        QueryDefinition? capturedQueryDefinition = null;

        mockContainer
            .Setup(c => c.GetItemQueryIterator<Selection>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((queryDefinition, _, _) => capturedQueryDefinition = queryDefinition)
            .Returns(feedIterator.Object);

        var itemResponse = new Mock<ItemResponse<Selection>>();
        itemResponse.Setup(r => r.Resource).Returns((Selection?)null!);

        mockContainer
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<Selection>(),
                It.IsAny<PartitionKey?>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemResponse.Object);

        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Selections")).Returns(mockContainer.Object);

        var repository = new CosmosSelectionRepository(mockCosmosClient.Object, mockConfiguration.Object);

        var incoming = new Selection
        {
            Id = Guid.Empty,
            RaceId = raceId,
            UserId = userId,
            BetType = BetType.PreQualy,
            Selections = ["leclerc", "norris", "hamilton", "piastri", "verstappen"],
            SubmittedAtUtc = DateTime.UtcNow
        };

        await repository.UpsertSelectionAsync(incoming);

        mockContainer.Verify(c => c.UpsertItemAsync(
            It.Is<Selection>(s =>
                s.Id == existingId &&
                s.RaceId == raceId &&
                s.UserId == userId &&
                s.Selections[0] == "leclerc"),
            It.Is<PartitionKey?>(pk => pk.HasValue && pk.Value.Equals(new PartitionKey(raceId))),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedQueryDefinition);
        Assert.Contains("c.RaceId", capturedQueryDefinition!.QueryText, StringComparison.Ordinal);
        Assert.Contains("c.UserId", capturedQueryDefinition.QueryText, StringComparison.Ordinal);
        Assert.Contains("ORDER BY c._ts DESC", capturedQueryDefinition.QueryText, StringComparison.Ordinal);
    }
}

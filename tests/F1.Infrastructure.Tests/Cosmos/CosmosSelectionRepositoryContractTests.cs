using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Infrastructure.Tests.Cosmos;

public class CosmosSelectionRepositoryContractTests : SelectionRepositoryContractTests
{
    protected override ISelectionRepository CreateEmptyRepository()
    {
        var (mockClient, mockContainer) = BuildMocks();
        SetupQueryReturns(mockContainer, []);
        SetupUpsertReturnsInput(mockContainer);
        return BuildRepository(mockClient.Object);
    }

    protected override Task<(ISelectionRepository Repo, Selection Existing)> ArrangeRepositoryWithSelection(
        string raceId, string userId, Selection selection)
    {
        var (mockClient, mockContainer) = BuildMocks();

        var seeded = new Selection
        {
            Id = selection.Id == Guid.Empty ? Guid.NewGuid() : selection.Id,
            RaceId = selection.RaceId,
            UserId = selection.UserId,
            BetType = selection.BetType,
            OrderedSelections = selection.OrderedSelections,
            SubmittedAtUtc = selection.SubmittedAtUtc,
            IsLocked = selection.IsLocked
        };

        SetupQueryReturns(mockContainer, [seeded]);
        SetupUpsertReturnsInput(mockContainer);

        var repo = BuildRepository(mockClient.Object);
        return Task.FromResult(((ISelectionRepository)repo, seeded));
    }

    private static (Mock<CosmosClient> Client, Mock<Container> Container) BuildMocks()
    {
        var mockContainer = new Mock<Container>();
        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(c => c.GetContainer("F12025", "Selections")).Returns(mockContainer.Object);
        return (mockClient, mockContainer);
    }

    private static void SetupQueryReturns(Mock<Container> mockContainer, List<Selection> items)
    {
        var feedResponse = new Mock<FeedResponse<Selection>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(items.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<Selection>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<Selection>(
                It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);
    }

    private static void SetupUpsertReturnsInput(Mock<Container> mockContainer)
    {
        mockContainer
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<Selection>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<Selection, PartitionKey, ItemRequestOptions, CancellationToken>((item, _, _, _) =>
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();
                var response = new Mock<ItemResponse<Selection>>();
                response.SetupGet(r => r.Resource).Returns(item);
                return Task.FromResult(response.Object);
            });
    }

    private static CosmosSelectionRepository BuildRepository(CosmosClient client)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");
        return new CosmosSelectionRepository(client, config.Object);
    }
}

using F1.Core.Interfaces;
using F1.Core.Models;
using F1.Infrastructure.Repositories;
using F1.Infrastructure.Tests.Contracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Infrastructure.Tests.Cosmos;

public class CosmosRaceMetadataRepositoryContractTests : RaceMetadataRepositoryContractTests
{
    protected override IMetadataTestRepository CreateEmptyRepository()
    {
        var (_, mockContainer, mockClient) = BuildMocks();
        SetupQueryReturns(mockContainer, null);
        return new CosmosMetadataTestRepository(BuildRepository(mockClient.Object));
    }

    protected override IMetadataTestRepository CreateRepositoryWithMetadata(string raceId, RaceQuestionMetadata metadata)
    {
        var (documentId, mockContainer, mockClient) = BuildMocks();
        var document = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = documentId,
            RaceId = raceId,
            ETag = "etag-initial",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = metadata.H2HQuestion,
                BonusQuestion = metadata.BonusQuestion,
                IsPublished = metadata.IsPublished,
                UpdatedAtUtc = metadata.UpdatedAtUtc
            }
        };

        SetupQueryReturns(mockContainer, document);

        var updatedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = document.Id,
            RaceId = document.RaceId,
            ETag = "etag-after-patch",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "New H2H",
                BonusQuestion = "New bonus",
                IsPublished = true,
                UpdatedAtUtc = new DateTime(2026, 3, 14, 12, 0, 0, DateTimeKind.Utc)
            }
        };
        SetupPatchReturns(mockContainer, updatedDocument);

        return new CosmosMetadataTestRepository(BuildRepository(mockClient.Object));
    }

    protected override IMetadataTestRepository CreateRepositoryWithRaceDocumentOnly(string raceId)
    {
        var (documentId, mockContainer, mockClient) = BuildMocks();
        var document = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = documentId,
            RaceId = raceId,
            ETag = "etag-initial",
            AdminQuestionMetadata = null
        };

        SetupQueryReturns(mockContainer, document);

        var updatedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = document.Id,
            RaceId = document.RaceId,
            ETag = "etag-after-patch",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument()
        };
        SetupPatchReturns(mockContainer, updatedDocument);

        return new CosmosMetadataTestRepository(BuildRepository(mockClient.Object));
    }

    private static (string DocumentId, Mock<Container> Container, Mock<CosmosClient> Client) BuildMocks()
    {
        var documentId = $"doc-{Guid.NewGuid():N}";
        var mockContainer = new Mock<Container>();
        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);
        return (documentId, mockContainer, mockClient);
    }

    private static void SetupQueryReturns(Mock<Container> mockContainer, CosmosRaceMetadataRepository.RaceDocument? document)
    {
        var items = document is null
            ? new List<CosmosRaceMetadataRepository.RaceDocument>()
            : new List<CosmosRaceMetadataRepository.RaceDocument> { document };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(items.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(
                It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);
    }

    private static void SetupPatchReturns(Mock<Container> mockContainer, CosmosRaceMetadataRepository.RaceDocument returnDocument)
    {
        var patchResponse = new Mock<ItemResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        patchResponse.SetupGet(r => r.Resource).Returns(returnDocument);

        mockContainer
            .Setup(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<IReadOnlyList<PatchOperation>>(),
                It.IsAny<PatchItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(patchResponse.Object);
    }

    private static CosmosRaceMetadataRepository BuildRepository(CosmosClient client)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");
        config.Setup(c => c["CosmosDb:ResultsContainerName"]).Returns("Results");
        config.Setup(c => c["CosmosDb:ResultsContainerPartitionKeyPath"]).Returns("/raceId");
        return new CosmosRaceMetadataRepository(client, config.Object);
    }

    private sealed class CosmosMetadataTestRepository(IRaceMetadataRepository repository) : IMetadataTestRepository
    {
        public IRaceMetadataRepository Repository => repository;
    }
}

using F1.Core.Models;
using F1.Infrastructure.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace F1.Api.Tests.Integration;

public class CosmosRaceMetadataRepositoryTests
{
    [Fact]
    public async Task GetMetadataAsync_ShouldReturnMappedMetadata_WhenDocumentContainsAdminMetadata()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var raceDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = raceId,
            RaceId = raceId,
            ETag = "etag-query",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Who finishes higher: Leclerc or Norris?",
                BonusQuestion = "How many safety-car laps?",
                IsPublished = true,
                UpdatedAtUtc = new DateTime(2025, 12, 7, 11, 0, 0, DateTimeKind.Utc)
            }
        };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<CosmosRaceMetadataRepository.RaceDocument> { raceDocument }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        QueryDefinition? capturedQueryDefinition = null;
        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string?, QueryRequestOptions?>((queryDefinition, _, _) => capturedQueryDefinition = queryDefinition)
            .Returns(feedIterator.Object);

        var repository = CreateRepository(mockCosmosClient.Object, "/id");

        var result = await repository.GetMetadataAsync(raceId);

        Assert.NotNull(result);
        Assert.Equal(raceId, result!.RaceId);
        Assert.Equal("Who finishes higher: Leclerc or Norris?", result.H2HQuestion);
        Assert.Equal("How many safety-car laps?", result.BonusQuestion);
        Assert.True(result.IsPublished);
        Assert.Equal("etag-query", result.ETag);

        Assert.NotNull(capturedQueryDefinition);
        Assert.Contains("c.id = @raceId OR c.raceId = @raceId", capturedQueryDefinition!.QueryText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldReturnNull_WhenAdminMetadataIsMissing()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var raceDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = raceId,
            RaceId = raceId,
            ETag = "etag-query",
            AdminQuestionMetadata = null
        };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<CosmosRaceMetadataRepository.RaceDocument> { raceDocument }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);

        var repository = CreateRepository(mockCosmosClient.Object, "/id");

        var result = await repository.GetMetadataAsync(raceId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertMetadataAsync_ShouldPatchAndReturnUpdatedMetadata()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var queriedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = "2025-24-yas_marina-document-id",
            RaceId = raceId,
            ETag = "etag-from-query",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Old H2H",
                BonusQuestion = "Old bonus",
                IsPublished = false,
                UpdatedAtUtc = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        var updatedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = queriedDocument.Id,
            RaceId = queriedDocument.RaceId,
            ETag = "etag-after-patch",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Who finishes higher: Leclerc or Norris?",
                BonusQuestion = "How many safety-car laps?",
                IsPublished = true,
                UpdatedAtUtc = new DateTime(2025, 12, 7, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<CosmosRaceMetadataRepository.RaceDocument> { queriedDocument }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);

        IReadOnlyList<PatchOperation>? capturedOperations = null;
        PartitionKey capturedPartitionKey = default;
        string? capturedId = null;

        var patchResponse = new Mock<ItemResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        patchResponse.SetupGet(r => r.Resource).Returns(updatedDocument);

        mockContainer
            .Setup(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<IReadOnlyList<PatchOperation>>(),
                It.IsAny<PatchItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, IReadOnlyList<PatchOperation>, PatchItemRequestOptions, CancellationToken>((id, partitionKey, operations, _, _) =>
            {
                capturedId = id;
                capturedPartitionKey = partitionKey;
                capturedOperations = operations;
            })
            .ReturnsAsync(patchResponse.Object);

        var repository = CreateRepository(mockCosmosClient.Object, "/raceId");

        var result = await repository.UpsertMetadataAsync(raceId, new RaceQuestionMetadata
        {
            RaceId = raceId,
            H2HQuestion = "Who finishes higher: Leclerc or Norris?",
            BonusQuestion = "How many safety-car laps?",
            IsPublished = true,
            UpdatedAtUtc = new DateTime(2025, 12, 7, 12, 0, 0, DateTimeKind.Utc)
        });

        Assert.NotNull(result);
        Assert.Equal("etag-after-patch", result.ETag);
        Assert.Equal("Who finishes higher: Leclerc or Norris?", result.H2HQuestion);
        Assert.Equal("How many safety-car laps?", result.BonusQuestion);

        Assert.Equal(queriedDocument.Id, capturedId);
        Assert.True(capturedPartitionKey.Equals(new PartitionKey(raceId)));

        Assert.NotNull(capturedOperations);
        Assert.Single(capturedOperations!);
        Assert.Equal(PatchOperationType.Set, capturedOperations![0].OperationType);
        Assert.Equal("/adminQuestionMetadata", capturedOperations[0].Path);
    }

    [Fact]
    public async Task UpsertMetadataAsync_ShouldUsePartitionKeyNoneUpFront_WhenRaceIdMissing()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var queriedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = raceId,
            RaceId = null,
            ETag = "etag-from-query",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Old",
                BonusQuestion = "Old",
                IsPublished = false,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<CosmosRaceMetadataRepository.RaceDocument> { queriedDocument }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);

        var updatedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = queriedDocument.Id,
            RaceId = queriedDocument.RaceId,
            ETag = "etag-after-patch",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Who finishes higher?",
                BonusQuestion = "How many DNFs?",
                IsPublished = true,
                UpdatedAtUtc = new DateTime(2025, 12, 7, 12, 0, 0, DateTimeKind.Utc)
            }
        };

        var patchResponse = new Mock<ItemResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        patchResponse.SetupGet(r => r.Resource).Returns(updatedDocument);

        mockContainer
            .Setup(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
                queriedDocument.Id,
                PartitionKey.None,
                It.IsAny<IReadOnlyList<PatchOperation>>(),
                It.IsAny<PatchItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(patchResponse.Object);

        var repository = CreateRepository(mockCosmosClient.Object, "/raceId");

        var result = await repository.UpsertMetadataAsync(raceId, new RaceQuestionMetadata
        {
            RaceId = raceId,
            H2HQuestion = "Who finishes higher?",
            BonusQuestion = "How many DNFs?",
            IsPublished = true,
            UpdatedAtUtc = new DateTime(2025, 12, 7, 12, 0, 0, DateTimeKind.Utc)
        });

        Assert.NotNull(result);
        Assert.Equal("etag-after-patch", result.ETag);

        mockContainer.Verify(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
            queriedDocument.Id,
            PartitionKey.None,
            It.IsAny<IReadOnlyList<PatchOperation>>(),
            It.IsAny<PatchItemRequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        mockContainer.Verify(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
            queriedDocument.Id,
            It.Is<PartitionKey>(pk => !pk.Equals(PartitionKey.None)),
            It.IsAny<IReadOnlyList<PatchOperation>>(),
            It.IsAny<PatchItemRequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertMetadataAsync_ShouldNotRetryWithPartitionKeyNone_WhenRaceIdIsEmptyString()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var queriedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = raceId,
            RaceId = string.Empty,
            ETag = "etag-from-query",
            AdminQuestionMetadata = new CosmosRaceMetadataRepository.AdminQuestionMetadataDocument
            {
                H2HQuestion = "Old",
                BonusQuestion = "Old",
                IsPublished = false,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        var feedResponse = new Mock<FeedResponse<CosmosRaceMetadataRepository.RaceDocument>>();
        feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<CosmosRaceMetadataRepository.RaceDocument> { queriedDocument }.GetEnumerator());

        var feedIterator = new Mock<FeedIterator<CosmosRaceMetadataRepository.RaceDocument>>();
        feedIterator.SetupSequence(i => i.HasMoreResults).Returns(true).Returns(false);
        feedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(feedResponse.Object);

        mockContainer
            .Setup(c => c.GetItemQueryIterator<CosmosRaceMetadataRepository.RaceDocument>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Returns(feedIterator.Object);

        mockContainer
            .Setup(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<IReadOnlyList<PatchOperation>>(),
                It.IsAny<PatchItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("pk mismatch", System.Net.HttpStatusCode.NotFound, 0, string.Empty, 0));

        var repository = CreateRepository(mockCosmosClient.Object, "/raceId");

        await Assert.ThrowsAsync<CosmosException>(() => repository.UpsertMetadataAsync(raceId, new RaceQuestionMetadata
        {
            RaceId = raceId,
            H2HQuestion = "Who finishes higher?",
            BonusQuestion = "How many DNFs?",
            IsPublished = true,
            UpdatedAtUtc = new DateTime(2025, 12, 7, 12, 0, 0, DateTimeKind.Utc)
        }));

        mockContainer.Verify(c => c.PatchItemAsync<CosmosRaceMetadataRepository.RaceDocument>(
            queriedDocument.Id,
            PartitionKey.None,
            It.IsAny<IReadOnlyList<PatchOperation>>(),
            It.IsAny<PatchItemRequestOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CosmosRaceMetadataRepository CreateRepository(CosmosClient cosmosClient, string partitionKeyPath)
    {
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["CosmosDb:DatabaseName"]).Returns("F12025");
        mockConfiguration.Setup(c => c["CosmosDb:ResultsContainerName"]).Returns("Results");
        mockConfiguration.Setup(c => c["CosmosDb:ResultsContainerPartitionKeyPath"]).Returns(partitionKeyPath);

        return new CosmosRaceMetadataRepository(cosmosClient, mockConfiguration.Object);
    }
}

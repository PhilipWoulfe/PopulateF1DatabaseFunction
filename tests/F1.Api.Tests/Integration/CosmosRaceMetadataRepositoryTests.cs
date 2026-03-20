using F1.Core.Exceptions;
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
    public async Task UpsertMetadataAsync_ShouldPatchWithExpectedEtag_AndReturnUpdatedMetadata()
    {
        var raceId = "2025-24-yas_marina";
        var expectedEtag = "etag-from-client";

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

        PatchItemRequestOptions? capturedOptions = null;
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
            .Callback<string, PartitionKey, IReadOnlyList<PatchOperation>, PatchItemRequestOptions, CancellationToken>((id, partitionKey, operations, options, _) =>
            {
                capturedId = id;
                capturedPartitionKey = partitionKey;
                capturedOperations = operations;
                capturedOptions = options;
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
        }, expectedEtag);

        Assert.NotNull(result);
        Assert.Equal("etag-after-patch", result.ETag);
        Assert.Equal("Who finishes higher: Leclerc or Norris?", result.H2HQuestion);
        Assert.Equal("How many safety-car laps?", result.BonusQuestion);

        Assert.Equal(queriedDocument.Id, capturedId);
        Assert.True(capturedPartitionKey.Equals(new PartitionKey(raceId)));
        Assert.NotNull(capturedOptions);
        Assert.Equal(expectedEtag, capturedOptions!.IfMatchEtag);

        Assert.NotNull(capturedOperations);
        Assert.Single(capturedOperations!);
        Assert.Equal(PatchOperationType.Set, capturedOperations![0].OperationType);
        Assert.Equal("/adminQuestionMetadata", capturedOperations[0].Path);
    }

    [Fact]
    public async Task UpsertMetadataAsync_ShouldThrowRaceMetadataConcurrencyException_WhenCosmosReturnsPreconditionFailed()
    {
        var raceId = "2025-24-yas_marina";

        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        mockCosmosClient.Setup(c => c.GetContainer("F12025", "Results")).Returns(mockContainer.Object);

        var queriedDocument = new CosmosRaceMetadataRepository.RaceDocument
        {
            Id = raceId,
            RaceId = raceId,
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
            .ThrowsAsync(new CosmosException("etag conflict", System.Net.HttpStatusCode.PreconditionFailed, 0, string.Empty, 0));

        var repository = CreateRepository(mockCosmosClient.Object, "/id");

        var ex = await Assert.ThrowsAsync<RaceMetadataConcurrencyException>(() => repository.UpsertMetadataAsync(raceId, new RaceQuestionMetadata
        {
            RaceId = raceId,
            H2HQuestion = "New H2H",
            BonusQuestion = "New bonus",
            IsPublished = true,
            UpdatedAtUtc = DateTime.UtcNow
        }, "etag-client"));

        Assert.Contains("update conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
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

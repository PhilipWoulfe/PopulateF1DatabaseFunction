using F1.Core.Interfaces;
using F1.Core.Exceptions;
using F1.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace F1.Infrastructure.Repositories;

public class CosmosRaceMetadataRepository : IRaceMetadataRepository
{
    private readonly Container _container;
    private readonly string _partitionKeyPath;

    public CosmosRaceMetadataRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var containerName = configuration["CosmosDb:ResultsContainerName"] ?? "Results";
        _partitionKeyPath = configuration["CosmosDb:ResultsContainerPartitionKeyPath"] ?? "/id";
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<RaceQuestionMetadata?> GetMetadataAsync(string raceId)
    {
        var document = await GetRaceDocumentAsync(raceId);
        return MapToMetadata(document);
    }

    public async Task<RaceQuestionMetadata> UpsertMetadataAsync(string raceId, RaceQuestionMetadata metadata, string? expectedEtag)
    {
        var document = await GetRaceDocumentAsync(raceId);
        if (document is null)
        {
            throw new KeyNotFoundException($"Race document not found for raceId '{raceId}'.");
        }

        var metadataDocument = new AdminQuestionMetadataDocument
        {
            H2HQuestion = metadata.H2HQuestion,
            BonusQuestion = metadata.BonusQuestion,
            IsPublished = metadata.IsPublished,
            UpdatedAtUtc = metadata.UpdatedAtUtc
        };

        var options = new PatchItemRequestOptions
        {
            IfMatchEtag = expectedEtag ?? document.ETag
        };

        try
        {
            var response = await _container.PatchItemAsync<RaceDocument>(
                document.Id,
                ResolvePartitionKey(document),
                [PatchOperation.Set("/adminQuestionMetadata", metadataDocument)],
                options);

            var updated = MapToMetadata(response.Resource);
            if (updated is null)
            {
                throw new InvalidOperationException("Race metadata was not returned after patch operation.");
            }

            return updated;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            throw new RaceMetadataConcurrencyException("Race metadata update conflict detected. Refresh and retry.", ex);
        }
    }

    private async Task<RaceDocument?> GetRaceDocumentAsync(string raceId)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT TOP 1 c.id, c.raceId, c._etag, c.adminQuestionMetadata FROM c WHERE c.id = @raceId OR c.raceId = @raceId")
            .WithParameter("@raceId", raceId);

        using var query = _container.GetItemQueryIterator<RaceDocument>(queryDefinition);
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            var document = response.FirstOrDefault();
            if (document is not null)
            {
                return document;
            }
        }

        return null;
    }

    private PartitionKey ResolvePartitionKey(RaceDocument document)
    {
        return _partitionKeyPath switch
        {
            "/raceId" => new PartitionKey(document.RaceId ?? document.Id),
            _ => new PartitionKey(document.Id)
        };
    }

    private static RaceQuestionMetadata? MapToMetadata(RaceDocument? document)
    {
        if (document?.AdminQuestionMetadata is null)
        {
            return null;
        }

        return new RaceQuestionMetadata
        {
            RaceId = document.RaceId ?? document.Id,
            H2HQuestion = document.AdminQuestionMetadata.H2HQuestion,
            BonusQuestion = document.AdminQuestionMetadata.BonusQuestion,
            IsPublished = document.AdminQuestionMetadata.IsPublished,
            UpdatedAtUtc = document.AdminQuestionMetadata.UpdatedAtUtc,
            ETag = document.ETag
        };
    }

    public class RaceDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("raceId")]
        public string? RaceId { get; set; }

        [JsonProperty("_etag")]
        public string? ETag { get; set; }

        [JsonProperty("adminQuestionMetadata")]
        public AdminQuestionMetadataDocument? AdminQuestionMetadata { get; set; }
    }

    public class AdminQuestionMetadataDocument
    {
        [JsonProperty("h2hQuestion")]
        public string H2HQuestion { get; set; } = string.Empty;

        [JsonProperty("bonusQuestion")]
        public string BonusQuestion { get; set; } = string.Empty;

        [JsonProperty("isPublished")]
        public bool IsPublished { get; set; }

        [JsonProperty("updatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; }
    }
}
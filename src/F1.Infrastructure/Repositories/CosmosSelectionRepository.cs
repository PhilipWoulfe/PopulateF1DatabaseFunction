using F1.Core.Interfaces;
using F1.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace F1.Infrastructure.Repositories;

public class CosmosSelectionRepository : ISelectionRepository
{
    private readonly Container _container;

    public CosmosSelectionRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"];
        _container = cosmosClient.GetContainer(databaseName, "Selections");
    }

    public async Task<Selection?> GetSelectionAsync(string raceId, string userId)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE (c.RaceId = @raceId OR c.raceId = @raceId) AND (c.UserId = @userId OR c.userId = @userId) ORDER BY c._ts DESC")
            .WithParameter("@raceId", raceId)
            .WithParameter("@userId", userId);

        var query = _container.GetItemQueryIterator<Selection>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(raceId)
            });

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            var selection = response.FirstOrDefault();
            if (selection is not null)
            {
                return selection;
            }
        }

        return null;
    }

    public async Task<Selection> UpsertSelectionAsync(Selection selection)
    {
        if (selection.Id == Guid.Empty)
        {
            var existing = await GetSelectionAsync(selection.RaceId, selection.UserId);
            selection.Id = existing?.Id ?? Guid.NewGuid();
        }

        var response = await _container.UpsertItemAsync(selection, new PartitionKey(selection.RaceId));
        return response.Resource;
    }
}

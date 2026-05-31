using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Repositories;

public class CosmosDbRepository<T> : ICosmosDbRepository<T> where T : class
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbRepository<T>> _logger;

    public CosmosDbRepository(
        CosmosClient cosmosClient,
        string databaseName,
        string containerName,
        ILogger<CosmosDbRepository<T>> logger)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(string id, string partitionKey)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Item with id {Id} not found", id);
            return null;
        }
    }

    public Task<IEnumerable<T>> GetItemsAsync(string queryString)
    {
        return GetItemsAsync(new QueryDefinition(queryString));
    }

    public async Task<IEnumerable<T>> GetItemsAsync(QueryDefinition queryDefinition)
    {
        var query = _container.GetItemQueryIterator<T>(queryDefinition);
        var results = new List<T>();

        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<T> CreateAsync(T item, string partitionKey)
    {
        var response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task<T> UpdateAsync(string id, T item, string partitionKey)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task DeleteAsync(string id, string partitionKey)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
    }
}

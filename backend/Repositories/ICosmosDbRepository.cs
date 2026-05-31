using Microsoft.Azure.Cosmos;

namespace AzureFunctionApp.Repositories;

public interface ICosmosDbRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, string partitionKey);
    Task<IEnumerable<T>> GetItemsAsync(string queryString);
    Task<IEnumerable<T>> GetItemsAsync(QueryDefinition queryDefinition);
    Task<T> CreateAsync(T item, string partitionKey);
    Task<T> UpdateAsync(string id, T item, string partitionKey);
    Task DeleteAsync(string id, string partitionKey);
}

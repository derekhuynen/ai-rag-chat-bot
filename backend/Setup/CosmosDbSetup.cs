using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Setup;

public class CosmosDbSetup
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly ILogger _logger;

    public CosmosDbSetup(CosmosClient cosmosClient, IConfiguration configuration, ILogger logger)
    {
        _cosmosClient = cosmosClient;
        _databaseName = configuration["CosmosDb:DatabaseName"] ?? "AIChatBot";
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting Cosmos DB initialization...");

            // Create database if it doesn't exist
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            _logger.LogInformation("Database {DatabaseName} ready", _databaseName);

            var database = databaseResponse.Database;

            // Create Users container (no throughput for serverless)
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = "Users",
                    PartitionKeyPath = "/id"
                }
            );
            _logger.LogInformation("Container 'Users' ready");

            // Create Conversations container (no throughput for serverless)
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = "Conversations",
                    PartitionKeyPath = "/userId"
                }
            );
            _logger.LogInformation("Container 'Conversations' ready");

            // Create Documents container (no throughput for serverless)
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = "Documents",
                    PartitionKeyPath = "/id"
                }
            );
            _logger.LogInformation("Container 'Documents' ready");

            _logger.LogInformation("Cosmos DB initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Cosmos DB");
            throw;
        }
    }
}

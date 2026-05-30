using AzureFunctionApp.Models;
using AzureFunctionApp.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserModel = AzureFunctionApp.Models.User;

namespace AzureFunctionApp.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly ICosmosDbRepository<UserModel> _userRepository;
    private readonly ICosmosDbRepository<Conversation> _conversationRepository;
    private readonly ICosmosDbRepository<Document> _documentRepository;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        ILogger<CosmosDbService> logger)
    {
        _logger = logger;

        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "AIChatBot";

        _userRepository = new CosmosDbRepository<UserModel>(
            cosmosClient,
            databaseName,
            "Users",
            loggerFactory.CreateLogger<CosmosDbRepository<UserModel>>());

        _conversationRepository = new CosmosDbRepository<Conversation>(
            cosmosClient,
            databaseName,
            "Conversations",
            loggerFactory.CreateLogger<CosmosDbRepository<Conversation>>());

        _documentRepository = new CosmosDbRepository<Document>(
            cosmosClient,
            databaseName,
            "Documents",
            loggerFactory.CreateLogger<CosmosDbRepository<Document>>());
    }

    // User operations
    public async Task<UserModel?> GetUserByIdAsync(string userId)
    {
        return await _userRepository.GetByIdAsync(userId, userId);
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
            .WithParameter("@email", email);
        var users = await _userRepository.GetItemsAsync(query);
        return users.FirstOrDefault();
    }

    public async Task<UserModel> CreateUserAsync(UserModel user)
    {
        return await _userRepository.CreateAsync(user, user.Id);
    }

    public async Task<UserModel> UpdateUserAsync(UserModel user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        return await _userRepository.UpdateAsync(user.Id, user, user.Id);
    }

    public async Task<IEnumerable<UserModel>> GetAllUsersAsync()
    {
        var query = "SELECT * FROM c ORDER BY c.createdAt DESC";
        return await _userRepository.GetItemsAsync(query);
    }

    // Conversation operations
    public async Task<Conversation?> GetConversationByIdAsync(string conversationId, string userId)
    {
        return await _conversationRepository.GetByIdAsync(conversationId, userId);
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId ORDER BY c.updatedAt DESC")
            .WithParameter("@userId", userId);
        return await _conversationRepository.GetItemsAsync(query);
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation)
    {
        return await _conversationRepository.CreateAsync(conversation, conversation.UserId);
    }

    public async Task<Conversation> UpdateConversationAsync(Conversation conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        return await _conversationRepository.UpdateAsync(conversation.Id, conversation, conversation.UserId);
    }

    public async Task DeleteConversationAsync(string conversationId, string userId)
    {
        await _conversationRepository.DeleteAsync(conversationId, userId);
    }

    public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
    {
        var query = "SELECT * FROM c ORDER BY c.createdAt DESC";
        return await _conversationRepository.GetItemsAsync(query);
    }

    // Message operations
    public async Task AddMessageToConversationAsync(string conversationId, string userId, Message message)
    {
        var conversation = await GetConversationByIdAsync(conversationId, userId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        message.ConversationId = conversationId;
        conversation.Messages.Add(message);
        conversation.UpdatedAt = DateTime.UtcNow;

        await UpdateConversationAsync(conversation);
    }

    public async Task<List<Message>> GetMessagesForConversationAsync(string conversationId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @conversationId")
            .WithParameter("@conversationId", conversationId);
        var conversations = await _conversationRepository.GetItemsAsync(query);
        var conversation = conversations.FirstOrDefault();

        return conversation?.Messages ?? new List<Message>();
    }

    // Document operations
    public async Task<Document> CreateDocumentAsync(Document document)
    {
        return await _documentRepository.CreateAsync(document, document.Id);
    }

    public async Task<Document?> GetDocumentByIdAsync(string documentId)
    {
        return await _documentRepository.GetByIdAsync(documentId, documentId);
    }

    public async Task<IEnumerable<Document>> GetAllDocumentsAsync(string? status = null)
    {
        var query = string.IsNullOrEmpty(status)
            ? "SELECT * FROM c ORDER BY c.uploadedAt DESC"
            : $"SELECT * FROM c WHERE c.status = '{status}' ORDER BY c.uploadedAt DESC";
        
        return await _documentRepository.GetItemsAsync(query);
    }

    public async Task<Document> UpdateDocumentAsync(Document document)
    {
        return await _documentRepository.UpdateAsync(document.Id, document, document.Id);
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        await _documentRepository.DeleteAsync(documentId, documentId);
    }
}

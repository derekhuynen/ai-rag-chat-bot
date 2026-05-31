using AzureFunctionApp.Models;
using UserModel = AzureFunctionApp.Models.User;

namespace AzureFunctionApp.Services;

public interface ICosmosDbService
{
    // User operations
    Task<UserModel?> GetUserByIdAsync(string userId);
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel> CreateUserAsync(UserModel user);
    Task<UserModel> UpdateUserAsync(UserModel user);
    Task<IEnumerable<UserModel>> GetAllUsersAsync();

    // Conversation operations
    Task<Conversation?> GetConversationByIdAsync(string conversationId, string userId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId);
    Task<Conversation> CreateConversationAsync(Conversation conversation);
    Task<Conversation> UpdateConversationAsync(Conversation conversation);
    Task DeleteConversationAsync(string conversationId, string userId);
    Task<IEnumerable<Conversation>> GetAllConversationsAsync();

    // Message operations
    Task AddMessageToConversationAsync(string conversationId, string userId, Message message);
    Task<List<Message>> GetMessagesForConversationAsync(string conversationId);

    // Document operations
    Task<Document> CreateDocumentAsync(Document document);
    Task<Document?> GetDocumentByIdAsync(string documentId);
    Task<IEnumerable<Document>> GetAllDocumentsAsync(string? status = null);
    Task<Document> UpdateDocumentAsync(Document document);
    Task DeleteDocumentAsync(string documentId);
}

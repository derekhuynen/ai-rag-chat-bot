using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public interface IAIService
{
    Task<string> GetChatCompletionAsync(string userMessage, List<Message> conversationHistory, List<DocumentAttachment>? documents = null);
    IAsyncEnumerable<string> GetStreamingChatCompletionAsync(string userMessage, List<Message> conversationHistory, List<ImageAttachment>? currentMessageImages = null, List<DocumentAttachment>? currentMessageDocuments = null);
    Task<IEnumerable<string>> GetAvailableModelsAsync();
}

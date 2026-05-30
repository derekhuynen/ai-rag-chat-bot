using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public class AIService : IAIService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ILogger<AIService> _logger;
    private readonly List<string> _availableModels;

    public AIService(IConfiguration configuration, TokenCredential credential, ILogger<AIService> logger)
    {
        _logger = logger;

        var endpoint = configuration["AzureAI:Endpoint"] ?? throw new InvalidOperationException("Azure AI endpoint not configured");
        var deploymentName = configuration["AzureAI:DeploymentName"] ?? "gpt-4";

        // Initialize available models (configure in app settings)
        var modelsConfig = configuration["AzureAI:AvailableModels"];
        _availableModels = string.IsNullOrEmpty(modelsConfig)
            ? new List<string> { deploymentName }
            : modelsConfig.Split(',').Select(m => m.Trim()).ToList();

        // Build Semantic Kernel (keyless)
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: endpoint,
            credentials: credential);

        _kernel = builder.Build();
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();

        _logger.LogInformation("AI Service initialized with model: {Model}", deploymentName);
    }

    public async Task<string> GetChatCompletionAsync(string userMessage, List<Message> conversationHistory, List<DocumentAttachment>? documents = null)
    {
        try
        {
            var chatHistory = BuildChatHistory(conversationHistory, userMessage, null, documents);
            var response = await _chatService.GetChatMessageContentAsync(chatHistory);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion");
            throw;
        }
    }

    public async IAsyncEnumerable<string> GetStreamingChatCompletionAsync(
        string userMessage,
        List<Message> conversationHistory,
        List<ImageAttachment>? currentMessageImages = null,
        List<DocumentAttachment>? currentMessageDocuments = null)
    {
        var chatHistory = BuildChatHistory(conversationHistory, userMessage, currentMessageImages, currentMessageDocuments);

        await foreach (var update in _chatService.GetStreamingChatMessageContentsAsync(chatHistory))
        {
            if (!string.IsNullOrEmpty(update.Content))
            {
                yield return update.Content;
            }
        }
    }

    private ChatHistory BuildChatHistory(
        List<Message> conversationHistory,
        string currentMessage,
        List<ImageAttachment>? currentMessageImages,
        List<DocumentAttachment>? currentMessageDocuments)
    {
        var chatHistory = new ChatHistory();

        // Add conversation history with images and documents if present
        foreach (var message in conversationHistory)
        {
            if (message.Role == "user")
            {
                var hasImages = message.Images != null && message.Images.Count > 0;
                var hasDocuments = message.Documents != null && message.Documents.Count > 0;

                if (hasImages || hasDocuments)
                {
                    var contentItems = new ChatMessageContentItemCollection();

                    // Add text content with document context
                    var messageText = message.Content;
                    if (hasDocuments)
                    {
                        messageText = AppendDocumentContext(message.Content, message.Documents!);
                    }
                    contentItems.Add(new TextContent(messageText));

                    // Add images
                    if (hasImages)
                    {
                        foreach (var image in message.Images!)
                        {
                            contentItems.Add(new ImageContent(new Uri(image.Url)));
                        }
                    }

                    chatHistory.AddUserMessage(contentItems);
                }
                else
                {
                    chatHistory.AddUserMessage(message.Content);
                }
            }
            else if (message.Role == "assistant")
            {
                chatHistory.AddAssistantMessage(message.Content);
            }
        }

        // Add current message with images and documents if present
        var hasCurrentImages = currentMessageImages != null && currentMessageImages.Count > 0;
        var hasCurrentDocuments = currentMessageDocuments != null && currentMessageDocuments.Count > 0;

        if (hasCurrentImages || hasCurrentDocuments)
        {
            var contentItems = new ChatMessageContentItemCollection();

            // Add text content with document context
            var messageText = currentMessage;
            if (hasCurrentDocuments)
            {
                messageText = AppendDocumentContext(currentMessage, currentMessageDocuments!);
            }
            contentItems.Add(new TextContent(messageText));

            // Add images
            if (hasCurrentImages)
            {
                foreach (var image in currentMessageImages!)
                {
                    contentItems.Add(new ImageContent(new Uri(image.Url)));
                }
            }

            chatHistory.AddUserMessage(contentItems);
        }
        else
        {
            chatHistory.AddUserMessage(currentMessage);
        }

        return chatHistory;
    }

    private string AppendDocumentContext(string userMessage, List<DocumentAttachment> documents)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(userMessage);
        sb.AppendLine();
        sb.AppendLine("Context from attached documents:");
        sb.AppendLine();

        foreach (var doc in documents)
        {
            sb.AppendLine($"Document: {doc.Filename}");
            if (doc.PageCount.HasValue)
            {
                sb.AppendLine($"Pages: {doc.PageCount.Value}");
            }
            sb.AppendLine($"Content:");
            sb.AppendLine(doc.ExtractedText);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_availableModels);
    }
}

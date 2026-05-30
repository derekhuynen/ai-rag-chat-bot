using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using AzureFunctionApp.Utils;
using System.Text.Json;

namespace AzureFunctionApp.Functions;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public List<string>? ConversationHistory { get; set; }
    public List<ImageAttachment>? Images { get; set; }
    public List<DocumentAttachment>? Documents { get; set; }
    public bool UseRAG { get; set; } = true;
}

public class ChatStreamFunction
{
    private readonly ILogger<ChatStreamFunction> _logger;
    private readonly IAIService _aiService;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IAuthenticationService _authService;
    private readonly IAISearchService _searchService;

    public ChatStreamFunction(
        ILogger<ChatStreamFunction> logger,
        IAIService aiService,
        ICosmosDbService cosmosDbService,
        IAuthenticationService authService,
        IAISearchService searchService)
    {
        _logger = logger;
        _aiService = aiService;
        _cosmosDbService = cosmosDbService;
        _authService = authService;
        _searchService = searchService;
    }

    [Function("ChatStream")]
    public async Task Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat/stream")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Chat stream endpoint called");

            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                req.HttpContext.Response.StatusCode = 400;
                await req.HttpContext.Response.WriteAsJsonAsync(new { error = "Message is required" });
                return;
            }

            // Validate images if present
            if (chatRequest.Images != null && chatRequest.Images.Count > 0)
            {
                var validation = ImageValidator.ValidateImages(chatRequest.Images);
                if (!validation.IsValid)
                {
                    req.HttpContext.Response.StatusCode = 400;
                    await req.HttpContext.Response.WriteAsJsonAsync(new { error = validation.ErrorMessage });
                    return;
                }
            }

            // Validate documents if present
            if (chatRequest.Documents != null && chatRequest.Documents.Count > 0)
            {
                if (chatRequest.Documents.Count > DocumentValidator.MaxDocumentsPerMessage)
                {
                    req.HttpContext.Response.StatusCode = 400;
                    await req.HttpContext.Response.WriteAsJsonAsync(new { error = $"Maximum {DocumentValidator.MaxDocumentsPerMessage} documents allowed per message" });
                    return;
                }
            }

            // Validate authentication
            if (!_authService.ValidateRequestToken(req, out string userId, out _, out _))
            {
                req.HttpContext.Response.StatusCode = 401;
                await req.HttpContext.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
                return;
            }

            // Set up SSE headers
            req.HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            req.HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
            req.HttpContext.Response.Headers.Append("Connection", "keep-alive");

            // Load full conversation history with images if conversation exists
            List<Message> messageHistory = new List<Message>();
            if (!string.IsNullOrEmpty(chatRequest.ConversationId))
            {
                var conversation = await _cosmosDbService.GetConversationByIdAsync(chatRequest.ConversationId, userId);
                if (conversation != null)
                {
                    var messages = await _cosmosDbService.GetMessagesForConversationAsync(chatRequest.ConversationId);
                    messageHistory = messages.OrderBy(m => m.CreatedAt).ToList();
                }
            }

            var fullResponse = "";
            List<DocumentCitation>? citations = null;

            // Perform RAG search if enabled
            if (chatRequest.UseRAG)
            {
                try
                {
                    var searchResults = await _searchService.SearchAsync(chatRequest.Message);
                    if (searchResults.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} relevant document chunks for RAG", searchResults.Count);

                        // Build context from search results
                        var contextBuilder = new System.Text.StringBuilder();
                        contextBuilder.AppendLine("\n\nRelevant information from documents:");
                        foreach (var result in searchResults)
                        {
                            contextBuilder.AppendLine($"\nFrom {result.FileName}:");
                            contextBuilder.AppendLine(result.Content);
                        }

                        // Append context to user message
                        var enhancedMessage = chatRequest.Message + contextBuilder.ToString();

                        // Stream AI response with enhanced context
                        await foreach (var chunk in _aiService.GetStreamingChatCompletionAsync(
                            enhancedMessage,
                            messageHistory,
                            chatRequest.Images,
                            chatRequest.Documents))
                        {
                            fullResponse += chunk;
                            var data = $"data: {JsonSerializer.Serialize(new { content = chunk })}\n\n";
                            await req.HttpContext.Response.WriteAsync(data);
                            await req.HttpContext.Response.Body.FlushAsync();
                        }

                        // Prepare citations
                        citations = searchResults.Select(r => new DocumentCitation
                        {
                            DocumentId = r.DocumentId,
                            DocumentName = r.FileName,
                            Page = r.Page,
                            BlobUrl = r.BlobUrl,
                            RelevanceScore = r.RelevanceScore
                        }).ToList();
                    }
                    else
                    {
                        // No relevant documents, stream normally
                        await foreach (var chunk in _aiService.GetStreamingChatCompletionAsync(
                            chatRequest.Message,
                            messageHistory,
                            chatRequest.Images,
                            chatRequest.Documents))
                        {
                            fullResponse += chunk;
                            var data = $"data: {JsonSerializer.Serialize(new { content = chunk })}\n\n";
                            await req.HttpContext.Response.WriteAsync(data);
                            await req.HttpContext.Response.Body.FlushAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RAG search failed, falling back to normal chat");
                    // Fall back to normal chat on error
                    await foreach (var chunk in _aiService.GetStreamingChatCompletionAsync(
                        chatRequest.Message,
                        messageHistory,
                        chatRequest.Images,
                        chatRequest.Documents))
                    {
                        fullResponse += chunk;
                        var data = $"data: {JsonSerializer.Serialize(new { content = chunk })}\n\n";
                        await req.HttpContext.Response.WriteAsync(data);
                        await req.HttpContext.Response.Body.FlushAsync();
                    }
                }
            }
            else
            {
                // RAG disabled, stream normally
                await foreach (var chunk in _aiService.GetStreamingChatCompletionAsync(
                    chatRequest.Message,
                    messageHistory,
                    chatRequest.Images,
                    chatRequest.Documents))
                {
                    fullResponse += chunk;
                    var data = $"data: {JsonSerializer.Serialize(new { content = chunk })}\n\n";
                    await req.HttpContext.Response.WriteAsync(data);
                    await req.HttpContext.Response.Body.FlushAsync();
                }
            }

            // Save conversation and messages if conversationId is provided
            if (!string.IsNullOrEmpty(chatRequest.ConversationId))
            {
                try
                {
                    var conversation = await _cosmosDbService.GetConversationByIdAsync(chatRequest.ConversationId, userId);

                    if (conversation != null)
                    {
                        // Add user message with images and documents
                        await _cosmosDbService.AddMessageToConversationAsync(
                            chatRequest.ConversationId,
                            userId,
                            new Message
                            {
                                Role = "user",
                                Content = chatRequest.Message,
                                Images = chatRequest.Images,
                                Documents = chatRequest.Documents
                            });

                        // Add AI response with citations
                        await _cosmosDbService.AddMessageToConversationAsync(
                            chatRequest.ConversationId,
                            userId,
                            new Message
                            {
                                Role = "assistant",
                                Content = fullResponse,
                                Citations = citations
                            });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving messages to conversation");
                    // Continue even if save fails
                }
            }

            // Send citations (if any) so the client can render them immediately
            if (citations != null && citations.Count > 0)
            {
                var citationsPayload = new { citations };
                var citationsData = $"data: {JsonSerializer.Serialize(citationsPayload)}\n\n";
                await req.HttpContext.Response.WriteAsync(citationsData);
                await req.HttpContext.Response.Body.FlushAsync();
            }

            // Send completion event
            await req.HttpContext.Response.WriteAsync("data: [DONE]\n\n");
            await req.HttpContext.Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat request");
            req.HttpContext.Response.StatusCode = 500;
            await req.HttpContext.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { error = "An error occurred" })}\n\n");
            await req.HttpContext.Response.Body.FlushAsync();
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using System.Text.Json;

namespace AzureFunctionApp.Functions;

public class ConversationFunction
{
    private readonly ILogger<ConversationFunction> _logger;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IAuthenticationService _authService;

    public ConversationFunction(
        ILogger<ConversationFunction> logger,
        ICosmosDbService cosmosDbService,
        IAuthenticationService authService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
        _authService = authService;
    }

    [Function("CreateConversation")]
    public async Task<IActionResult> CreateConversation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "conversations")] HttpRequest req)
    {
        try
        {
            if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
            {
                return errorResult!;
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateConversationRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var conversation = new Conversation
            {
                UserId = userId,
                Title = request?.Title ?? "New conversation",
                ModelName = request?.ModelName ?? "gpt-4.1"
            };

            var created = await _cosmosDbService.CreateConversationAsync(conversation);
            return new OkObjectResult(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            return new ObjectResult(new { error = "Failed to create conversation" }) { StatusCode = 500 };
        }
    }

    [Function("GetConversations")]
    public async Task<IActionResult> GetConversations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "conversations")] HttpRequest req)
    {
        try
        {
            if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
            {
                return errorResult!;
            }

            var conversations = await _cosmosDbService.GetUserConversationsAsync(userId);
            return new OkObjectResult(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations");
            return new ObjectResult(new { error = "Failed to retrieve conversations" }) { StatusCode = 500 };
        }
    }

    [Function("GetConversation")]
    public async Task<IActionResult> GetConversation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "conversations/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
            {
                return errorResult!;
            }

            var conversation = await _cosmosDbService.GetConversationByIdAsync(id, userId);

            if (conversation == null)
            {
                return new NotFoundObjectResult(new { error = "Conversation not found" });
            }

            return new OkObjectResult(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation {ConversationId}", id);
            return new ObjectResult(new { error = "Failed to retrieve conversation" }) { StatusCode = 500 };
        }
    }

    [Function("DeleteConversation")]
    public async Task<IActionResult> DeleteConversation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "conversations/{id}")] HttpRequest req,
        string id)
    {
        try
        {
            if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
            {
                return errorResult!;
            }

            await _cosmosDbService.DeleteConversationAsync(id, userId);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
            return new ObjectResult(new { error = "Failed to delete conversation" }) { StatusCode = 500 };
        }
    }
}

public class CreateConversationRequest
{
    public string? Title { get; set; }
    public string? ModelName { get; set; }
}

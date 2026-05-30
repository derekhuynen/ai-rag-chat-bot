using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using System.Text.Json;

namespace AzureFunctionApp.Functions;

public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly IAIService _aiService;
    private readonly IAuthenticationService _authService;

    public ChatFunction(ILogger<ChatFunction> logger, IAIService aiService, IAuthenticationService authService)
    {
        _logger = logger;
        _aiService = aiService;
        _authService = authService;
    }

    [Function("Chat")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Chat endpoint called");

            // Require a valid bearer token (this endpoint drives the paid AI backend).
            if (!_authService.ValidateRequestAndGetUserId(req, out _, out var authError))
            {
                return authError!;
            }

            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                return new BadRequestObjectResult(new { error = "Message is required" });
            }

            // Get AI response (ChatFunction is deprecated, use ChatStreamFunction instead)
            // This function doesn't support images - redirect users to use streaming endpoint
            var response = await _aiService.GetChatCompletionAsync(
                chatRequest.Message,
                new List<Message>() // Empty history for this deprecated endpoint
            );

            var chatResponse = new ChatResponse
            {
                Message = response,
                ConversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            return new OkObjectResult(chatResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = 500
            };
        }
    }
}

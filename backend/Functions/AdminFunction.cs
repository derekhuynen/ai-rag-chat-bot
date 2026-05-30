using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;

namespace AzureFunctionApp.Functions;

public class AdminFunction
{
    private readonly ILogger<AdminFunction> _logger;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IAuthenticationService _authService;

    public AdminFunction(
        ILogger<AdminFunction> logger,
        ICosmosDbService cosmosDbService,
        IAuthenticationService authService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
        _authService = authService;
    }

    [Function("AdminGetUsers")]
    public async Task<IActionResult> AdminGetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/users")] HttpRequest req)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var users = await _cosmosDbService.GetAllUsersAsync();

            var userList = users.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                name = u.Name,
                role = u.Role.ToString(),
                createdAt = u.CreatedAt
            });

            return new OkObjectResult(userList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new ObjectResult(new { error = "Failed to get users" }) { StatusCode = 500 };
        }
    }

    [Function("AdminGetStats")]
    public async Task<IActionResult> AdminGetStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/stats")] HttpRequest req)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var users = await _cosmosDbService.GetAllUsersAsync();
            var totalUsers = users.Count();

            var allConversations = await _cosmosDbService.GetAllConversationsAsync();
            var totalConversations = allConversations.Count();

            // Calculate total messages
            var totalMessages = allConversations.Sum(c => c.Messages.Count);

            return new OkObjectResult(new
            {
                totalUsers,
                totalConversations,
                totalMessages,
                usersWithConversations = allConversations.Select(c => c.UserId).Distinct().Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return new ObjectResult(new { error = "Failed to get stats" }) { StatusCode = 500 };
        }
    }
}

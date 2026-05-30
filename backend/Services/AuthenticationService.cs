using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICosmosDbService _cosmosDbService;

    public AuthenticationService(IJwtTokenService jwtTokenService, ICosmosDbService cosmosDbService)
    {
        _jwtTokenService = jwtTokenService;
        _cosmosDbService = cosmosDbService;
    }

    public bool ValidateRequestToken(HttpRequest req, out string userId, out string email, out UserRole role)
    {
        userId = string.Empty;
        email = string.Empty;
        role = UserRole.User;

        if (!req.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return false;
        }

        var token = authHeader.ToString().Replace("Bearer ", "");
        return _jwtTokenService.ValidateToken(token, out userId, out email, out role);
    }

    public async Task<IActionResult?> ValidateAdminRequestAsync(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return new UnauthorizedObjectResult(new { error = "Authorization header is required" });
        }

        var token = authHeader.ToString().Replace("Bearer ", "");
        if (!_jwtTokenService.ValidateToken(token, out var userId, out _, out _))
        {
            return new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
        }

        // Re-verify the role against current DB state rather than trusting the token's
        // role claim, whichcovers demotion, deletion, and stale long-lived tokens.
        var user = await _cosmosDbService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
        }

        if (user.Role != UserRole.Admin)
        {
            return new ObjectResult(new { error = "Admin access required" }) { StatusCode = 403 };
        }

        return null;
    }

    public bool ValidateRequestAndGetUserId(HttpRequest req, out string userId, out IActionResult? errorResult)
    {
        userId = string.Empty;
        errorResult = null;

        if (!req.Headers.TryGetValue("Authorization", out var authHeader))
        {
            errorResult = new UnauthorizedObjectResult(new { error = "Authorization header is required" });
            return false;
        }

        var token = authHeader.ToString().Replace("Bearer ", "");
        if (!_jwtTokenService.ValidateToken(token, out userId, out _, out _))
        {
            errorResult = new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            return false;
        }

        return true;
    }

    public System.Security.Claims.ClaimsPrincipal? ValidateAndGetPrincipal(string token)
    {
        return _jwtTokenService.GetPrincipalFromToken(token);
    }
}

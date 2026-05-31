using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public interface IAuthenticationService
{
    /// <summary>
    /// Validates JWT token from request and extracts user information
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <param name="userId">Output user ID if valid</param>
    /// <param name="email">Output email if valid</param>
    /// <param name="role">Output role if valid</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateRequestToken(HttpRequest req, out string userId, out string email, out UserRole role);

    /// <summary>
    /// Validates the JWT token and confirms the user is currently an admin by
    /// re-checking the role against the database (not just the token claim).
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <returns>Null if the caller is a valid admin; otherwise the error result to return.</returns>
    Task<IActionResult?> ValidateAdminRequestAsync(HttpRequest req);

    /// <summary>
    /// Validates JWT token and returns user ID
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <param name="userId">Output user ID if valid</param>
    /// <param name="errorResult">Output error result if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateRequestAndGetUserId(HttpRequest req, out string userId, out IActionResult? errorResult);

    /// <summary>
    /// Validates JWT token and returns claims principal
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? ValidateAndGetPrincipal(string token);
}

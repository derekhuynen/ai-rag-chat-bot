using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token, out string userId, out string email, out UserRole role);
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromToken(string token);
}

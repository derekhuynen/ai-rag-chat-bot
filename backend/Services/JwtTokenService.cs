using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AzureFunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AzureFunctionApp.Services;

public class JwtTokenService : IJwtTokenService
{
    // HMAC-SHA256 requires a key of at least 256 bits (32 bytes) to be secure.
    private const int MinSecretKeyBytes = 32;

    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _logger = logger;
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

        if (Encoding.UTF8.GetByteCount(_secretKey) < MinSecretKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey must be at least {MinSecretKeyBytes} bytes ({MinSecretKeyBytes * 8} bits) for HMAC-SHA256.");
        }

        _issuer = configuration["Jwt:Issuer"] ?? "AIChatBot";
        _audience = configuration["Jwt:Audience"] ?? "AIChatBot";
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "1440"); // Default 24 hours
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token, out string userId, out string email, out UserRole role)
    {
        userId = string.Empty;
        email = string.Empty;
        role = UserRole.User;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // JWT uses short claim names, not full ClaimTypes URIs
            var nameIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier);
            var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "email" || x.Type == ClaimTypes.Email);
            var roleClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "role" || x.Type == ClaimTypes.Role);

            if (nameIdClaim == null || emailClaim == null || roleClaim == null)
            {
                _logger.LogWarning("JWT validation failed: missing required claims");
                return false;
            }

            userId = nameIdClaim.Value;
            email = emailClaim.Value;
            role = Enum.Parse<UserRole>(roleClaim.Value);

            return true;
        }
        catch (Exception ex)
        {
            // Log at Debug without token contents to avoid leaking token material.
            _logger.LogDebug(ex, "JWT validation failed");
            return false;
        }
    }

    public System.Security.Claims.ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AzureFunctionApp.Tests;

public class JwtTokenServiceTests
{
    private const string ValidSecret = "this-is-a-test-secret-key-with-enough-length-1234567890";

    private static IConfiguration BuildConfig(
        string? secret = ValidSecret,
        string issuer = "AIChatBot",
        string audience = "AIChatBot",
        string expirationMinutes = "1440")
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = issuer,
            ["Jwt:Audience"] = audience,
            ["Jwt:ExpirationMinutes"] = expirationMinutes,
        };
        if (secret != null)
        {
            settings["Jwt:SecretKey"] = secret;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static JwtTokenService CreateService(IConfiguration config)
        => new(config, NullLogger<JwtTokenService>.Instance);

    private static User SampleUser() => new()
    {
        Id = "user-123",
        Email = "user@example.com",
        Name = "Test User",
        Role = UserRole.Admin,
    };

    [Fact]
    public void GenerateThenValidate_RoundTrips_UserIdEmailAndRole()
    {
        var service = CreateService(BuildConfig());
        var user = SampleUser();

        var token = service.GenerateToken(user);
        var ok = service.ValidateToken(token, out var userId, out var email, out var role);

        Assert.True(ok);
        Assert.Equal(user.Id, userId);
        Assert.Equal(user.Email, email);
        Assert.Equal(UserRole.Admin, role);
    }

    [Fact]
    public void Constructor_TooShortSecretKey_ThrowsInvalidOperationException()
    {
        // 16 bytes < 32 byte minimum for HMAC-SHA256.
        var config = BuildConfig(secret: "short-secret-123");

        Assert.Throws<InvalidOperationException>(() => CreateService(config));
    }

    [Fact]
    public void Constructor_MissingSecretKey_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(secret: null);

        Assert.Throws<InvalidOperationException>(() => CreateService(config));
    }

    // Builds a token that is already expired, signed with the same secret/issuer
    // the service validates against, so only the lifetime check should reject it.
    private static string BuildExpiredToken(string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString()),
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = DateTime.UtcNow.AddMinutes(-5),
            Issuer = "AIChatBot",
            Audience = "AIChatBot",
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
        });
        return handler.WriteToken(token);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsFalse()
    {
        var service = CreateService(BuildConfig());
        var expiredToken = BuildExpiredToken(ValidSecret);

        var ok = service.ValidateToken(expiredToken, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ValidateToken_TokenSignedWithDifferentKey_ReturnsFalse()
    {
        var signer = CreateService(BuildConfig(secret: ValidSecret));
        var token = signer.GenerateToken(SampleUser());

        var otherKeyService = CreateService(
            BuildConfig(secret: "a-completely-different-secret-key-abcdefghijklmnop"));

        var ok = otherKeyService.ValidateToken(token, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void ValidateToken_GarbageToken_ReturnsFalse()
    {
        var service = CreateService(BuildConfig());

        var ok = service.ValidateToken("not-a-real-token", out _, out _, out _);

        Assert.False(ok);
    }
}

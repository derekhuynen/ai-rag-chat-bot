using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AzureFunctionApp.Tests;

public class AuthenticationServiceTests
{
    private static HttpRequest BuildRequest(string? authHeader = null)
    {
        var context = new DefaultHttpContext();
        if (authHeader != null)
        {
            context.Request.Headers["Authorization"] = authHeader;
        }
        return context.Request;
    }

    private static AuthenticationService CreateService(
        out Mock<IJwtTokenService> jwt,
        out Mock<ICosmosDbService> cosmos)
    {
        jwt = new Mock<IJwtTokenService>();
        cosmos = new Mock<ICosmosDbService>();
        return new AuthenticationService(jwt.Object, cosmos.Object);
    }

    // Helper to set up ValidateToken with out-params returning the given role.
    private static void SetupValidToken(
        Mock<IJwtTokenService> jwt, string userId, UserRole role, bool valid = true)
    {
        jwt.Setup(j => j.ValidateToken(
                It.IsAny<string>(),
                out It.Ref<string>.IsAny,
                out It.Ref<string>.IsAny,
                out It.Ref<UserRole>.IsAny))
            .Returns((string _, out string uid, out string email, out UserRole r) =>
            {
                uid = userId;
                email = "user@example.com";
                r = role;
                return valid;
            });
    }

    [Fact]
    public async Task MissingAuthorizationHeader_Returns401()
    {
        var service = CreateService(out _, out _);

        var result = await service.ValidateAdminRequestAsync(BuildRequest());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var service = CreateService(out var jwt, out _);
        SetupValidToken(jwt, "user-1", UserRole.Admin, valid: false);

        var result = await service.ValidateAdminRequestAsync(BuildRequest("Bearer bad-token"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ValidTokenButUserNotInDb_Returns401()
    {
        var service = CreateService(out var jwt, out var cosmos);
        SetupValidToken(jwt, "user-1", UserRole.Admin);
        cosmos.Setup(c => c.GetUserByIdAsync("user-1")).ReturnsAsync((User?)null);

        var result = await service.ValidateAdminRequestAsync(BuildRequest("Bearer good-token"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task TokenClaimsAdminButDbUserDemoted_Returns403()
    {
        var service = CreateService(out var jwt, out var cosmos);
        // Token validates as Admin...
        SetupValidToken(jwt, "user-1", UserRole.Admin);
        // ...but the DB says this user is now only a regular User.
        cosmos.Setup(c => c.GetUserByIdAsync("user-1"))
            .ReturnsAsync(new User { Id = "user-1", Role = UserRole.User });

        var result = await service.ValidateAdminRequestAsync(BuildRequest("Bearer good-token"));

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    [Fact]
    public async Task ValidAdminUser_ReturnsNull()
    {
        var service = CreateService(out var jwt, out var cosmos);
        SetupValidToken(jwt, "user-1", UserRole.Admin);
        cosmos.Setup(c => c.GetUserByIdAsync("user-1"))
            .ReturnsAsync(new User { Id = "user-1", Role = UserRole.Admin });

        var result = await service.ValidateAdminRequestAsync(BuildRequest("Bearer good-token"));

        Assert.Null(result);
    }
}

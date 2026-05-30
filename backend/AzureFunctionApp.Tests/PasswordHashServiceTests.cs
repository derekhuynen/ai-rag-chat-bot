using AzureFunctionApp.Services;
using Xunit;

namespace AzureFunctionApp.Tests;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _service = new();

    [Fact]
    public void HashPassword_DoesNotReturnPlaintext()
    {
        const string password = "S3cur3P@ssw0rd!";

        var hash = _service.HashPassword(password);

        Assert.NotEqual(password, hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        const string password = "S3cur3P@ssw0rd!";
        var hash = _service.HashPassword(password);

        Assert.True(_service.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _service.HashPassword("S3cur3P@ssw0rd!");

        Assert.False(_service.VerifyPassword("wrong-password", hash));
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        const string password = "S3cur3P@ssw0rd!";

        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        // BCrypt salts each hash, so identical input yields distinct hashes
        // that both still verify.
        Assert.NotEqual(hash1, hash2);
        Assert.True(_service.VerifyPassword(password, hash1));
        Assert.True(_service.VerifyPassword(password, hash2));
    }
}

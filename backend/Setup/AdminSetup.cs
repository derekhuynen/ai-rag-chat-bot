using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Setup;

public class AdminSetup
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public AdminSetup(
        ICosmosDbService cosmosDbService,
        IPasswordHashService passwordHashService,
        IConfiguration configuration,
        ILogger logger)
    {
        _cosmosDbService = cosmosDbService;
        _passwordHashService = passwordHashService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CreateAdminUserAsync()
    {
        try
        {
            var adminEmail = _configuration["Admin:Email"] ?? "admin@aichatbot.com";
            var adminPassword = _configuration["Admin:Password"] ?? "Admin@123456";
            var adminName = _configuration["Admin:Name"] ?? "Administrator";

            // Check if admin already exists
            var existingAdmin = await _cosmosDbService.GetUserByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists");
                return;
            }

            // Create admin user
            var adminUser = new User
            {
                Email = adminEmail,
                Name = adminName,
                PasswordHash = _passwordHashService.HashPassword(adminPassword),
                Role = UserRole.Admin
            };

            await _cosmosDbService.CreateUserAsync(adminUser);
            _logger.LogInformation("Admin user created successfully with email: {Email}", adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
        }
    }
}

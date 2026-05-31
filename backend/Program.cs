using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Azure.Core;
using Azure.Identity;
using AzureFunctionApp.Services;
using AzureFunctionApp.Setup;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});

// Single keyless credential shared by every Azure client (Managed Identity in
// cloud, az login locally). One place owns the auth policy and the token cache.
builder.Services.AddSingleton<TokenCredential>(new DefaultAzureCredential());

// Register Cosmos DB Client (keyless)
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpoint = configuration["CosmosDb:Endpoint"];

    if (string.IsNullOrEmpty(endpoint))
    {
        throw new InvalidOperationException("CosmosDb:Endpoint is not configured");
    }

    return new CosmosClient(endpoint, sp.GetRequiredService<TokenCredential>());
});

// Register services
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<IAIService, AIService>();
builder.Services.AddSingleton<IPasswordHashService, PasswordHashService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IDocumentParserService, DocumentParserService>();
builder.Services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddSingleton<IAISearchService, AISearchService>();

// Temporarily disabled Application Insights
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

var app = builder.Build();

// Initialize Cosmos DB containers on startup
using (var scope = app.Services.CreateScope())
{
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Initialize database containers
    var cosmosSetup = new CosmosDbSetup(cosmosClient, configuration, logger);
    await cosmosSetup.InitializeAsync();

    // Create admin user
    var cosmosDbService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();
    var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
    var adminSetup = new AdminSetup(cosmosDbService, passwordHashService, configuration, logger);
    await adminSetup.CreateAdminUserAsync();
}

app.Run();

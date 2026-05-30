---
applyTo: '/backend/**'
description: Backend development rules for Azure Functions .NET 10.0 application
---

# Backend Development Rules

## Project Overview

This is an Azure Functions v4 application using the .NET 10.0 isolated worker model with Cosmos DB and Azure AI integration.

## Core Principles

### 1. Use Existing Services - DO NOT Duplicate Code

**CRITICAL**: Before writing authentication or common logic, check if a service already exists:

- ✅ **Use `IAuthenticationService`** for all JWT validation
- ✅ **Use `ICosmosDbService`** for all database operations
- ✅ **Use `IJwtTokenService`** for token generation/validation
- ✅ **Use `IPasswordHashService`** for password operations
- ✅ **Use `IAIService`** for AI completions

**❌ NEVER write inline JWT validation code**
**❌ NEVER create direct Cosmos DB queries in functions**
**❌ NEVER implement password hashing in endpoints**

### 2. Authentication Pattern

**All protected endpoints MUST follow this exact pattern:**

```csharp
[Function("EndpointName")]
public async Task<IActionResult> EndpointName(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "path")] HttpRequest req)
{
    try
    {
        // For regular authenticated endpoints
        if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
        {
            return errorResult!;
        }

        // For admin-only endpoints
        if (!_authService.ValidateAdminRequest(req, out var errorResult))
        {
            return errorResult!;
        }

        // Business logic here
        var result = await _someService.DoSomethingAsync(userId);
        return new OkObjectResult(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error message");
        return new ObjectResult(new { error = "User-friendly error message" }) { StatusCode = 500 };
    }
}
```

### 3. Dependency Injection

**Constructor Pattern:**

```csharp
public class MyFunction
{
    private readonly ILogger<MyFunction> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ICosmosDbService _cosmosDbService;

    public MyFunction(
        ILogger<MyFunction> logger,
        IAuthenticationService authService,
        ICosmosDbService cosmosDbService)
    {
        _logger = logger;
        _authService = authService;
        _cosmosDbService = cosmosDbService;
    }
}
```

**Rules:**

- ✅ Always inject services via constructor
- ✅ Always inject `ILogger<T>` as first parameter
- ✅ Use interface types, not concrete implementations
- ❌ Never use `new` to instantiate services
- ❌ Never use static service references

### 4. Error Handling

**Consistent Error Responses:**

```csharp
// 400 Bad Request
return new BadRequestObjectResult(new { error = "Message is required" });

// 401 Unauthorized
return new UnauthorizedObjectResult(new { error = "Invalid or expired token" });

// 403 Forbidden
return new ObjectResult(new { error = "Admin access required" }) { StatusCode = 403 };

// 404 Not Found
return new NotFoundObjectResult(new { error = "Resource not found" });

// 409 Conflict
return new ConflictObjectResult(new { error = "Resource already exists" });

// 500 Internal Server Error
_logger.LogError(ex, "Context about what failed");
return new ObjectResult(new { error = "Failed to perform action" }) { StatusCode = 500 };
```

**Rules:**

- ✅ Always wrap endpoint logic in try-catch
- ✅ Always log exceptions with context using `_logger.LogError(ex, ...)`
- ✅ Return user-friendly error messages (never expose internal details)
- ✅ Use consistent error object format: `{ "error": "message" }`

### 5. Async/Await Pattern

**Rules:**

- ✅ ALL I/O operations must be async (database, HTTP, AI calls)
- ✅ Use `async Task<IActionResult>` for function signatures
- ✅ Always await async calls, never use `.Result` or `.Wait()`
- ✅ Use `ConfigureAwait(false)` in services (not in functions)

```csharp
// ✅ Correct
public async Task<IActionResult> MyFunction([HttpTrigger(...)] HttpRequest req)
{
    var result = await _service.GetDataAsync();
    return new OkObjectResult(result);
}

// ❌ Wrong - blocking call
var result = _service.GetDataAsync().Result;

// ❌ Wrong - not async
public IActionResult MyFunction([HttpTrigger(...)] HttpRequest req)
```

### 6. Function Structure

**File Organization:**

```
Functions/
├── AuthFunction.cs         # Authentication endpoints only
├── ConversationFunction.cs # Conversation CRUD only
├── ChatStreamFunction.cs   # Chat streaming only
├── AdminFunction.cs        # Admin operations only
└── [Feature]Function.cs    # One feature per file
```

**Rules:**

- ✅ One function class per feature area
- ✅ Related endpoints in the same file
- ✅ Maximum 5-6 endpoints per function class
- ❌ Don't mix unrelated endpoints in one file

### 7. Route Naming Conventions

**RESTful Routes:**

```csharp
// Resource collection
Route = "conversations"           // GET, POST
Route = "conversations/{id}"      // GET, PUT, DELETE

// Nested resources
Route = "conversations/{id}/messages"

// Actions/Operations
Route = "auth/login"
Route = "auth/register"
Route = "admin/stats"

// Streaming
Route = "chat/stream"
```

**Rules:**

- ✅ Use plural nouns for resources (`conversations`, not `conversation`)
- ✅ Use lowercase with hyphens for multi-word routes
- ✅ Use verbs only for actions that don't fit REST (login, register)
- ❌ Don't use verbs in REST routes (`/getConversations` is wrong)

### 8. Service Implementation Rules

**When Creating New Services:**

```csharp
// 1. Always create interface first
public interface IMyService
{
    Task<Result> DoSomethingAsync(string parameter);
}

// 2. Implement interface
public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IConfiguration _configuration;

    public MyService(ILogger<MyService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result> DoSomethingAsync(string parameter)
    {
        // Implementation
    }
}

// 3. Register in Program.cs
builder.Services.AddSingleton<IMyService, MyService>();
```

**Rules:**

- ✅ Always create interface + implementation
- ✅ Use dependency injection
- ✅ Register as Singleton unless stateful
- ✅ Inject ILogger and IConfiguration as needed
- ❌ Never instantiate services with `new`

### 9. Cosmos DB Patterns

**All database operations MUST go through CosmosDbService:**

```csharp
// ✅ Correct - use service
var conversations = await _cosmosDbService.GetUserConversationsAsync(userId);

// ❌ Wrong - direct Cosmos DB access
var container = cosmosClient.GetContainer(...);
var query = "SELECT * FROM c";
```

**Query Patterns:**

- ✅ Always filter by partition key (userId) for user data
- ✅ Use parameterized queries to prevent injection
- ✅ Handle `null` results gracefully
- ✅ Use `JsonProperty` attributes for property name mapping

### 10. Model Conventions

**Model Properties:**

```csharp
public class MyModel
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Rules:**

- ✅ Use `[JsonProperty]` with camelCase for Cosmos DB
- ✅ Initialize required properties with defaults
- ✅ Use `DateTime.UtcNow` for timestamps
- ✅ Always include `id`, `createdAt`, `updatedAt`
- ✅ Include partition key property (usually `userId`)

### 11. Configuration Access

**Reading Configuration:**

```csharp
// In services
private readonly string _cosmosEndpoint;

public MyService(IConfiguration configuration)
{
    _cosmosEndpoint = configuration["CosmosDb:Endpoint"]
        ?? throw new InvalidOperationException("CosmosDb:Endpoint not configured");
}
```

**Rules:**

- ✅ Read configuration in constructor
- ✅ Throw meaningful exceptions for missing config
- ✅ Use colon notation for nested keys: `"Section:Key"`
- ✅ Store config values as private readonly fields
- ❌ Don't read configuration in endpoint methods

### 12. Logging Best Practices

**Structured Logging:**

```csharp
// ✅ Good - structured with context
_logger.LogInformation("Processing request for user {UserId}", userId);
_logger.LogError(ex, "Failed to create conversation for user {UserId}", userId);

// ❌ Bad - string concatenation
_logger.LogInformation("Processing request for user " + userId);

// ✅ Different log levels
_logger.LogDebug("Debug information");
_logger.LogInformation("Normal flow");
_logger.LogWarning("Warning condition");
_logger.LogError(ex, "Error occurred");
```

**Rules:**

- ✅ Use structured logging with placeholders
- ✅ Include exception object in error logs
- ✅ Add context (userId, conversationId, etc.)
- ✅ Use appropriate log levels
- ❌ Don't log sensitive data (passwords, full tokens)

### 13. JWT and Security

**Token Handling:**

- ✅ Always use `IAuthenticationService` for validation
- ✅ Token expiration is configured in settings (default 24 hours)
- ✅ Always return userId from JWT claims, never from request body
- ❌ Never trust client-provided userId without JWT validation
- ❌ Never log full tokens (only first few characters for debugging)

**Password Security:**

- ✅ Always use `IPasswordHashService` for hashing
- ✅ BCrypt automatically handles salt generation
- ❌ Never store plain text passwords
- ❌ Never log passwords

### 14. HTTP Response Patterns

**Success Responses:**

```csharp
// Simple success
return new OkResult();  // 200 with no body

// Success with data
return new OkObjectResult(data);  // 200 with JSON body

// Created resource
return new CreatedResult($"/api/resource/{id}", data);  // 201
```

**Error Responses:**

```csharp
// Client errors (4xx)
return new BadRequestObjectResult(new { error = "..." });    // 400
return new UnauthorizedObjectResult(new { error = "..." });  // 401
return new ObjectResult(new { error = "..." }) { StatusCode = 403 };  // 403
return new NotFoundObjectResult(new { error = "..." });      // 404
return new ConflictObjectResult(new { error = "..." });      // 409

// Server errors (5xx)
return new ObjectResult(new { error = "..." }) { StatusCode = 500 };
```

### 15. Streaming Responses (SSE)

**For Server-Sent Events endpoints:**

```csharp
// Set headers
req.HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
req.HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
req.HttpContext.Response.Headers.Append("Connection", "keep-alive");

// Stream data
var data = $"data: {JsonSerializer.Serialize(new { content = chunk })}\n\n";
await req.HttpContext.Response.WriteAsync(data);
await req.HttpContext.Response.Body.FlushAsync();
```

**Rules:**

- ✅ Set proper SSE headers before streaming
- ✅ Format: `data: <json>\n\n`
- ✅ Flush after each chunk
- ✅ Handle errors gracefully during streaming

## Common Anti-Patterns to Avoid

### ❌ Don't Do This:

```csharp
// 1. Inline JWT validation
if (!req.Headers.TryGetValue("Authorization", out var authHeader))
{
    return new UnauthorizedObjectResult(new { error = "..." });
}

// 2. Direct Cosmos DB queries
var container = cosmosClient.GetContainer(...);

// 3. Synchronous I/O
var result = File.ReadAllText("file.txt");

// 4. Missing error handling
public async Task<IActionResult> MyFunction(...)
{
    var result = await _service.DoSomethingAsync();  // No try-catch!
    return new OkObjectResult(result);
}

// 5. Exposing internal errors
catch (Exception ex)
{
    return new ObjectResult(new { error = ex.Message });  // Leaks internals!
}
```

### ✅ Do This Instead:

```csharp
// 1. Use authentication service
if (!_authService.ValidateRequestAndGetUserId(req, out string userId, out var errorResult))
{
    return errorResult!;
}

// 2. Use Cosmos DB service
var result = await _cosmosDbService.GetUserConversationsAsync(userId);

// 3. Async I/O
var result = await File.ReadAllTextAsync("file.txt");

// 4. Proper error handling
public async Task<IActionResult> MyFunction(...)
{
    try
    {
        var result = await _service.DoSomethingAsync();
        return new OkObjectResult(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to do something");
        return new ObjectResult(new { error = "Failed to perform action" }) { StatusCode = 500 };
    }
}

// 5. User-friendly errors
catch (Exception ex)
{
    _logger.LogError(ex, "Context about error");
    return new ObjectResult(new { error = "Failed to complete operation" }) { StatusCode = 500 };
}
```

## Code Review Checklist

Before committing backend code, verify:

- [ ] Uses existing services (no duplicate logic)
- [ ] Proper authentication using `IAuthenticationService`
- [ ] Try-catch blocks with proper error handling
- [ ] All I/O operations are async
- [ ] Structured logging with context
- [ ] Consistent error response format
- [ ] Services registered in Program.cs
- [ ] Interface + implementation for new services
- [ ] No hardcoded configuration values
- [ ] No sensitive data in logs
- [ ] Proper HTTP status codes
- [ ] Clean build with no warnings

## Adding New Features

When adding a new feature:

1. **Identify the feature category** (Auth, Data, Admin, etc.)
2. **Create service if needed** (Interface + Implementation)
3. **Register service** in Program.cs
4. **Create or update function** with proper authentication
5. **Add error handling** and logging
6. **Test authentication** flow
7. **Update this documentation** if patterns change

## Performance Guidelines

- Use `Singleton` lifetime for stateless services
- Use `Scoped` only if service maintains per-request state
- Avoid `Transient` unless necessary
- Cache configuration values in service constructors
- Use streaming for large responses
- Optimize Cosmos DB queries with partition keys

## Security Checklist

- [ ] All endpoints except `/auth/register` and `/auth/login` require authentication
- [ ] Admin endpoints check for Admin role
- [ ] All database queries filter by userId from JWT
- [ ] Passwords are hashed with BCrypt
- [ ] JWT tokens have expiration
- [ ] No sensitive data in error messages
- [ ] No sensitive data in logs
- [ ] CORS configured for specific origins

## Documentation Requirements

When adding/modifying endpoints:

1. Update API endpoint table in architecture docs
2. Document request/response formats
3. Add authentication requirements
4. Document error responses
5. Add code examples for common patterns

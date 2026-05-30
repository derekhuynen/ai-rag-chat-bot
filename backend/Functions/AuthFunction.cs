using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using System.Text.Json;

namespace AzureFunctionApp.Functions;

public class AuthFunction
{
    private readonly ILogger<AuthFunction> _logger;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthFunction(
        ILogger<AuthFunction> logger,
        ICosmosDbService cosmosDbService,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
    }

    [Function("Register")]
    public async Task<IActionResult> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<RegisterRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return new BadRequestObjectResult(new { error = "Email and password are required" });
            }

            // Check if user already exists
            var existingUser = await _cosmosDbService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new ConflictObjectResult(new { error = "User with this email already exists" });
            }

            // Create new user
            var user = new User
            {
                Email = request.Email,
                Name = request.Name ?? request.Email.Split('@')[0],
                PasswordHash = _passwordHashService.HashPassword(request.Password),
                Role = UserRole.User
            };

            var createdUser = await _cosmosDbService.CreateUserAsync(user);

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(createdUser);

            return new OkObjectResult(new
            {
                token,
                user = new
                {
                    id = createdUser.Id,
                    email = createdUser.Email,
                    name = createdUser.Name,
                    role = createdUser.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return new ObjectResult(new { error = "Failed to register user" }) { StatusCode = 500 };
        }
    }

    [Function("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<LoginRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return new BadRequestObjectResult(new { error = "Email and password are required" });
            }

            // Find user by email
            var user = await _cosmosDbService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid email or password" });
            }

            // Verify password
            if (!_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new UnauthorizedObjectResult(new { error = "Invalid email or password" });
            }

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user);

            return new OkObjectResult(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.Name,
                    role = user.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user");
            return new ObjectResult(new { error = "Failed to login" }) { StatusCode = 500 };
        }
    }

    [Function("GetMe")]
    public async Task<IActionResult> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequest req)
    {
        try
        {
            // Extract token from Authorization header
            if (!req.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return new UnauthorizedObjectResult(new { error = "Authorization header missing" });
            }

            var token = authHeader.ToString().Replace("Bearer ", "");

            // Validate token and extract user info
            if (!_jwtTokenService.ValidateToken(token, out string userId, out string email, out UserRole role))
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            }

            // Get user from database
            var user = await _cosmosDbService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(new { error = "User not found" });
            }

            return new OkObjectResult(new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                role = user.Role.ToString(),
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return new ObjectResult(new { error = "Failed to get user info" }) { StatusCode = 500 };
        }
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

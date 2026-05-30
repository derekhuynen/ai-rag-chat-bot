using Newtonsoft.Json;

namespace AzureFunctionApp.Models;

public class User
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("role")]
    public UserRole Role { get; set; } = UserRole.User;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

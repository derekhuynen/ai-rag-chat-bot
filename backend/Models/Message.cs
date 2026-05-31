using Newtonsoft.Json;

namespace AzureFunctionApp.Models;

public class Message
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty; // "user" or "assistant"

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("images")]
    public List<ImageAttachment>? Images { get; set; }

    [JsonProperty("documents")]
    public List<DocumentAttachment>? Documents { get; set; }

    [JsonProperty("citations")]
    public List<DocumentCitation>? Citations { get; set; }
}

namespace AzureFunctionApp.Models;

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

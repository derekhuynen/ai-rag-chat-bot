namespace AzureFunctionApp.Models;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public List<string> ConversationHistory { get; set; } = new();
    public bool UseRAG { get; set; } = true; // Enable RAG by default
}

using System.Text.Json.Serialization;

namespace AzureFunctionApp.Models;

public class DocumentCitation
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("blobUrl")]
    public string BlobUrl { get; set; } = string.Empty;

    [JsonPropertyName("relevanceScore")]
    public double RelevanceScore { get; set; }
}

using Newtonsoft.Json;

namespace AzureFunctionApp.Models;

public class Document
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonProperty("originalFileName")]
    public string OriginalFileName { get; set; } = string.Empty;

    [JsonProperty("uploadedBy")]
    public string UploadedBy { get; set; } = string.Empty;

    [JsonProperty("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [JsonProperty("fileSize")]
    public long FileSize { get; set; }

    [JsonProperty("mimeType")]
    public string MimeType { get; set; } = "text/plain";

    [JsonProperty("blobUrl")]
    public string BlobUrl { get; set; } = string.Empty;

    /// <summary>
    /// The storage path of the blob within the container (e.g. "documents/processed/...").
    /// Persisted so we don't have to re-parse it out of <see cref="BlobUrl"/>. May be null/empty
    /// for legacy records created before this field existed; callers should fall back to parsing
    /// <see cref="BlobUrl"/> in that case.
    /// </summary>
    [JsonProperty("blobPath")]
    public string? BlobPath { get; set; }

    [JsonProperty("status")]
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    [JsonProperty("processingError")]
    public string? ProcessingError { get; set; }

    [JsonProperty("totalPages")]
    public int TotalPages { get; set; } = 1;

    [JsonProperty("totalChunks")]
    public int TotalChunks { get; set; } = 0;

    [JsonProperty("aiSearchDocIds")]
    public List<string> AiSearchDocIds { get; set; } = new();

    [JsonProperty("metadata")]
    public DocumentMetadata Metadata { get; set; } = new();
}

public enum DocumentStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}

public class DocumentMetadata
{
    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonProperty("sourceUrl")]
    public string? SourceUrl { get; set; }
}

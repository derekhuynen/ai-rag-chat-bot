using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public interface IAISearchService
{
    Task<bool> CreateIndexAsync();
    Task<bool> IndexDocumentChunksAsync(string documentId, List<DocumentChunk> chunks);
    Task<List<SearchResult>> SearchAsync(string query, List<string>? filterDocumentIds = null);
    Task DeleteDocumentChunksAsync(string documentId);
}

public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public float[] ContentVector { get; set; } = Array.Empty<float>();
    public float[] SummaryVector { get; set; } = Array.Empty<float>();
    public int ChunkIndex { get; set; }
    public int Page { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}

public class SearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Page { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

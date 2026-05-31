using AzureFunctionApp.Services;

namespace AzureFunctionApp.Services;

public interface IDocumentProcessingService
{
    Task<List<DocumentChunk>> ChunkDocumentAsync(string content, string documentId, string fileName);
    Task<string> SummarizeChunkAsync(string chunkContent);
    Task<float[]> GetEmbeddingAsync(string text);
}

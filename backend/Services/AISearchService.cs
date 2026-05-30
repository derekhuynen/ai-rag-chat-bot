using Azure;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Services;

public class AISearchService : IAISearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<AISearchService> _logger;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly string _indexName;
    private readonly double _minRelevanceScore;
    private readonly int _maxResults;

    public AISearchService(
        IConfiguration configuration,
        TokenCredential credential,
        IDocumentProcessingService documentProcessingService,
        ILogger<AISearchService> logger)
    {
        _logger = logger;
        _documentProcessingService = documentProcessingService;

        var searchEndpoint = configuration["AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException("AzureSearch:Endpoint not configured");
        _indexName = configuration["AzureSearch:IndexName"] ?? "ai-chat-documents";
        _minRelevanceScore = double.Parse(configuration["RAG:MinRelevanceScore"] ?? "0.7");
        _maxResults = int.Parse(configuration["RAG:MaxResults"] ?? "3");

        _indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
        _searchClient = _indexClient.GetSearchClient(_indexName);

        _logger.LogInformation("AISearchService initialized with index: {IndexName}", _indexName);
    }

    public async Task<bool> CreateIndexAsync()
    {
        try
        {
            _logger.LogInformation("Creating search index: {IndexName}", _indexName);

            // Index creation is handled by the PowerShell script
            // This method can be used for programmatic creation if needed

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating search index");
            return false;
        }
    }

    public async Task<bool> IndexDocumentChunksAsync(string documentId, List<DocumentChunk> chunks)
    {
        try
        {
            _logger.LogInformation("Indexing {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);

            var searchDocuments = chunks.Select(chunk => new SearchDocument
            {
                ["id"] = chunk.Id,
                ["documentId"] = chunk.DocumentId,
                ["fileName"] = chunk.FileName,
                ["content"] = chunk.Content,
                ["summary"] = chunk.Summary,
                ["contentVector"] = chunk.ContentVector,
                ["summaryVector"] = chunk.SummaryVector,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["page"] = chunk.Page,
                ["uploadedBy"] = chunk.UploadedBy,
                ["uploadedAt"] = chunk.UploadedAt,
                ["blobUrl"] = chunk.BlobUrl
            }).ToList();

            var batch = IndexDocumentsBatch.Upload(searchDocuments);
            var result = await _searchClient.IndexDocumentsAsync(batch);

            var failedCount = result.Value.Results.Count(r => !r.Succeeded);
            if (failedCount > 0)
            {
                _logger.LogWarning("{FailedCount} documents failed to index", failedCount);
                return false;
            }

            _logger.LogInformation("Successfully indexed {ChunkCount} chunks", chunks.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document chunks for {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<List<SearchResult>> SearchAsync(string query, List<string>? filterDocumentIds = null)
    {
        try
        {
            _logger.LogInformation("Searching for: {Query}", query);

            var searchOptions = new SearchOptions
            {
                Size = 10, // Get more results initially for scoring
                IncludeTotalCount = false
            };

            // Add fields to select
            searchOptions.Select.Add("id");
            searchOptions.Select.Add("documentId");
            searchOptions.Select.Add("fileName");
            searchOptions.Select.Add("content");
            searchOptions.Select.Add("summary");
            searchOptions.Select.Add("page");
            searchOptions.Select.Add("blobUrl");

            // Add filter if document IDs specified
            if (filterDocumentIds?.Any() == true)
            {
                var filter = string.Join(" or ", filterDocumentIds.Select(id => $"documentId eq '{id}'"));
                searchOptions.Filter = filter;
            }

            // Hybrid search: combine keyword (BM25) with vector (KNN over contentVector).
            // We embed the query text using the same embedding model/path used at indexing
            // time so the query vector is comparable to the stored contentVector values.
            // Azure AI Search fuses the keyword and vector result sets via Reciprocal Rank
            // Fusion (RRF) to produce the final ranking.
            try
            {
                var queryVector = await _documentProcessingService.GetEmbeddingAsync(query);
                if (queryVector.Length > 0)
                {
                    searchOptions.VectorSearch = new VectorSearchOptions();
                    searchOptions.VectorSearch.Queries.Add(new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = _maxResults,
                        Fields = { "contentVector" }
                    });
                }
            }
            catch (Exception ex)
            {
                // If embedding generation fails, degrade gracefully to keyword-only search
                // rather than failing the entire query.
                _logger.LogWarning(ex, "Failed to generate query embedding; falling back to keyword-only search");
            }

            var searchResults = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);

            var results = new List<SearchResult>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                var score = result.Score ?? 0;

                // NOTE: With hybrid search, @search.score is an RRF-fused score, NOT a
                // normalized 0-1 cosine/relevance value. RRF scores are typically small
                // (roughly 0.0-0.05 with the default rank constant) and are not comparable
                // to the legacy ">= 0.7" threshold, which would discard every result.
                // We therefore do NOT apply a hard score cutoff here and instead rely on
                // RRF ranking + the Take(_maxResults) cap below to select the best matches.
                // (_minRelevanceScore is retained in config for potential future use.)
                results.Add(new SearchResult
                {
                    DocumentId = result.Document["documentId"]?.ToString() ?? string.Empty,
                    FileName = result.Document["fileName"]?.ToString() ?? string.Empty,
                    Content = result.Document["content"]?.ToString() ?? string.Empty,
                    Summary = result.Document["summary"]?.ToString() ?? string.Empty,
                    Page = Convert.ToInt32(result.Document["page"]),
                    BlobUrl = result.Document["blobUrl"]?.ToString() ?? string.Empty,
                    RelevanceScore = score
                });
            }

            // Results already arrive in RRF rank order; take the top N.
            var topResults = results
                .OrderByDescending(r => r.RelevanceScore)
                .Take(_maxResults)
                .ToList();

            _logger.LogInformation(
                "Found {ResultCount} results (filtered to top {MaxResults}); configured MinRelevanceScore={MinScore} is not applied in hybrid/RRF mode",
                results.Count, topResults.Count, _minRelevanceScore);

            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return new List<SearchResult>();
        }
    }

    public async Task DeleteDocumentChunksAsync(string documentId)
    {
        try
        {
            _logger.LogInformation("Deleting chunks for document {DocumentId}", documentId);

            // Search for all chunks belonging to this document
            var searchOptions = new SearchOptions
            {
                Filter = $"documentId eq '{documentId}'",
                Size = 1000 // Max chunks per document
            };
            searchOptions.Select.Add("id");

            var searchResults = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);

            var documentsToDelete = new List<SearchDocument>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                documentsToDelete.Add(new SearchDocument
                {
                    ["id"] = result.Document["id"]
                });
            }

            if (documentsToDelete.Any())
            {
                var batch = IndexDocumentsBatch.Delete(documentsToDelete);
                await _searchClient.IndexDocumentsAsync(batch);
                _logger.LogInformation("Deleted {Count} chunks for document {DocumentId}",
                    documentsToDelete.Count, documentId);
            }
            else
            {
                _logger.LogInformation("No chunks found for document {DocumentId}", documentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document chunks for {DocumentId}", documentId);
            throw;
        }
    }
}

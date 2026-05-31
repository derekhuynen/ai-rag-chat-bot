using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using System.Text.Json;
using System.Security.Claims;

namespace AzureFunctionApp.Functions;

public class DocumentManagementFunction
{
    private readonly ILogger<DocumentManagementFunction> _logger;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IAuthenticationService _authService;
    private readonly IAISearchService _searchService;
    private readonly IConfiguration _configuration;
    private readonly IDocumentProcessingService _processingService;

    public DocumentManagementFunction(
        ILogger<DocumentManagementFunction> logger,
        ICosmosDbService cosmosDbService,
        IBlobStorageService blobStorageService,
        IAuthenticationService authService,
        IAISearchService searchService,
        IConfiguration configuration,
        IDocumentProcessingService processingService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
        _blobStorageService = blobStorageService;
        _authService = authService;
        _searchService = searchService;
        _configuration = configuration;
        _processingService = processingService;
    }

    [Function("UploadDocument")]
    public async Task<IActionResult> UploadDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "management/documents/upload")] HttpRequest req)
    {
        try
        {
            // Validate admin request
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            // Get user ID from token
            if (!req.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return new UnauthorizedObjectResult(new { error = "Authorization header required" });
            }

            var token = authHeader.ToString().Replace("Bearer ", "");
            var principal = _authService.ValidateAndGetPrincipal(token);
            if (principal == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid token" });
            }

            // Tokens are generated with ClaimTypes.Email; when read back via principal,
            // the claim type is the full URI, not the short "email" name
            var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? principal.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return new UnauthorizedObjectResult(new { error = "Email not found in token" });
            }

            // Check if request has file
            if (!req.HasFormContentType || !req.Form.Files.Any())
            {
                return new BadRequestObjectResult(new { error = "No file uploaded" });
            }

            var file = req.Form.Files[0];
            var maxFileSize = long.Parse(_configuration["DocumentProcessing:MaxFileSize"] ?? "10485760");

            // Validate file
            if (file.Length == 0)
            {
                return new BadRequestObjectResult(new { error = "Empty file" });
            }

            if (file.Length > maxFileSize)
            {
                return new BadRequestObjectResult(new { error = $"File too large. Max size: {maxFileSize / 1024 / 1024}MB" });
            }

            // Support plain text and markdown files
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".txt" && extension != ".md")
            {
                return new BadRequestObjectResult(new { error = "Only .txt and .md files are supported" });
            }

            // Generate unique filename with timestamp (preserve extension)
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var sanitizedFilename = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "-")
                .Replace("_", "-")
                .ToLower();
            var fileName = $"{timestamp}_{sanitizedFilename}{extension}";
            var blobPath = $"documents/pending/{fileName}";

            // Upload to blob storage
            string blobUrl;
            using (var stream = file.OpenReadStream())
            {
                // Treat markdown as text/plain for processing
                blobUrl = await _blobStorageService.UploadBlobAsync(stream, blobPath, "text/plain");
            }

            // Parse metadata from form (optional)
            DocumentMetadata metadata = new();
            if (req.Form.TryGetValue("metadata", out var metadataJson))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<DocumentMetadata>(metadataJson.ToString()) ?? new();
                }
                catch
                {
                    _logger.LogWarning("Invalid metadata JSON, using defaults");
                }
            }

            // Create document record
            var document = new Document
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                UploadedBy = userEmail,
                UploadedAt = DateTime.UtcNow,
                FileSize = file.Length,
                MimeType = "text/plain",
                BlobUrl = blobUrl,
                BlobPath = blobPath,
                Status = DocumentStatus.Pending,
                Metadata = metadata
            };

            await _cosmosDbService.CreateDocumentAsync(document);

            // Synchronously process the document instead of relying on a queue trigger
            await ProcessDocumentAsync(document);

            _logger.LogInformation("Document uploaded and processed: {DocumentId} by {User}", document.Id, userEmail);

            return new OkObjectResult(new
            {
                documentId = document.Id,
                fileName = document.FileName,
                status = document.Status.ToString().ToLower(),
                processingError = document.ProcessingError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return new ObjectResult(new { error = "Failed to upload document" }) { StatusCode = 500 };
        }
    }

    private async Task ProcessDocumentAsync(Document document)
    {
        string? documentId = document.Id;

        try
        {
            _logger.LogInformation("Processing document {DocumentId} synchronously after upload", documentId);

            // Update status to processing
            document.Status = DocumentStatus.Processing;
            await _cosmosDbService.UpdateDocumentAsync(document);

            // Download blob content
            _logger.LogInformation("Downloading blob for document {DocumentId}: {BlobUrl}", documentId, document.BlobUrl);

            var blobPath = ResolveBlobPath(document);
            var content = await _blobStorageService.DownloadBlobTextAsync(blobPath);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Document content is empty");
            }

            _logger.LogInformation("Downloaded {ByteCount} characters for document {DocumentId}", content.Length, documentId);

            // Step 1: Chunk the document
            _logger.LogInformation("Chunking document {DocumentId} (size: {Length} chars)...", documentId, content.Length);
            var chunks = await _processingService.ChunkDocumentAsync(content, documentId!, document.FileName);

            if (!chunks.Any())
            {
                throw new InvalidOperationException("No chunks generated from document");
            }

            document.TotalChunks = chunks.Count;
            _logger.LogInformation("Created {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);

            // Step 2: Process each chunk (summarize and vectorize)
            _logger.LogInformation("Processing {ChunkCount} chunks for document {DocumentId}...", chunks.Count, documentId);
            var processedChunks = new List<DocumentChunk>();

            foreach (var chunk in chunks)
            {
                try
                {
                    // Generate summary
                    _logger.LogDebug("Summarizing chunk {ChunkIndex} for document {DocumentId}", chunk.ChunkIndex, documentId);
                    chunk.Summary = await _processingService.SummarizeChunkAsync(chunk.Content ?? string.Empty);

                    // Generate embeddings for content and summary
                    _logger.LogDebug("Generating embeddings for chunk {ChunkIndex} for document {DocumentId}", chunk.ChunkIndex, documentId);
                    chunk.ContentVector = await _processingService.GetEmbeddingAsync(chunk.Content ?? string.Empty);
                    chunk.SummaryVector = await _processingService.GetEmbeddingAsync(chunk.Summary ?? string.Empty);

                    // Add metadata
                    chunk.UploadedBy = document.UploadedBy;
                    chunk.UploadedAt = document.UploadedAt;
                    chunk.BlobUrl = document.BlobUrl;

                    processedChunks.Add(chunk);

                    _logger.LogDebug("Processed chunk {ChunkIndex}/{TotalChunks} for document {DocumentId}",
                        chunk.ChunkIndex + 1, chunks.Count, documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chunk {ChunkIndex} for document {DocumentId}. Chunk content length: {Length}",
                        chunk.ChunkIndex, documentId, chunk.Content?.Length ?? 0);
                    // Continue with other chunks
                }
            }

            if (!processedChunks.Any())
            {
                throw new InvalidOperationException("No chunks successfully processed");
            }

            // Step 3: Index chunks in AI Search
            _logger.LogInformation("Indexing {ChunkCount} chunks in AI Search for document {DocumentId}...", processedChunks.Count, documentId);
            var indexed = await _searchService.IndexDocumentChunksAsync(documentId!, processedChunks);

            if (!indexed)
            {
                throw new InvalidOperationException("Failed to index document chunks");
            }

            // Store AI Search document IDs
            document.AiSearchDocIds = processedChunks.Select(c => c.Id).ToList();

            // Step 4: Move blob to processed folder
            _logger.LogInformation("Moving blob to processed folder for document {DocumentId}...", documentId);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var newBlobPath = $"documents/processed/{timestamp}_{document.FileName}";

            using var processedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var newBlobUrl = await _blobStorageService.UploadBlobAsync(
                processedStream,
                newBlobPath,
                document.MimeType
            );

            // Delete old blob
            await _blobStorageService.DeleteBlobAsync(blobPath);

            // Update document status
            document.BlobUrl = newBlobUrl;
            document.BlobPath = newBlobPath;
            document.Status = DocumentStatus.Processed;
            document.ProcessingError = null;
            await _cosmosDbService.UpdateDocumentAsync(document);

            _logger.LogInformation("Document {DocumentId} processed successfully", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId} synchronously", documentId);

            // Update document status to failed
            if (!string.IsNullOrEmpty(documentId))
            {
                try
                {
                    var latest = await _cosmosDbService.GetDocumentByIdAsync(documentId);
                    if (latest != null)
                    {
                        latest.Status = DocumentStatus.Failed;
                        latest.ProcessingError = ex.Message;
                        await _cosmosDbService.UpdateDocumentAsync(latest);

                        // Also update the in-memory instance so the HTTP response reflects the failure
                        document.Status = latest.Status;
                        document.ProcessingError = latest.ProcessingError;
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error updating document status to failed for {DocumentId}", documentId);
                }
            }
        }
    }

    /// <summary>
    /// Resolves the storage path of a document's blob. Prefers the persisted
    /// <see cref="Document.BlobPath"/>; for legacy records where it is null/empty, falls back
    /// to parsing it out of the blob URL.
    /// </summary>
    private static string ResolveBlobPath(Document document)
    {
        if (!string.IsNullOrEmpty(document.BlobPath))
        {
            return document.BlobPath;
        }

        // Backward-compat: derive the path from the URL (e.g. ".../ai-chat/documents/...").
        return document.BlobUrl.Split("/ai-chat/")[1];
    }

    [Function("ListDocuments")]
    public async Task<IActionResult> ListDocuments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/documents")] HttpRequest req)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var status = req.Query["status"].ToString();
            var documents = await _cosmosDbService.GetAllDocumentsAsync(status);

            var result = documents.Select(d => new
            {
                id = d.Id,
                fileName = d.FileName,
                originalFileName = d.OriginalFileName,
                uploadedBy = d.UploadedBy,
                uploadedAt = d.UploadedAt,
                fileSize = d.FileSize,
                status = d.Status.ToString().ToLower(),
                totalChunks = d.TotalChunks,
                processingError = d.ProcessingError
            });

            return new OkObjectResult(new { documents = result, total = result.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents");
            return new ObjectResult(new { error = "Failed to list documents" }) { StatusCode = 500 };
        }
    }

    [Function("GetDocument")]
    public async Task<IActionResult> GetDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/documents/{documentId}")] HttpRequest req,
        string documentId)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var document = await _cosmosDbService.GetDocumentByIdAsync(documentId);
            if (document == null)
            {
                return new NotFoundObjectResult(new { error = "Document not found" });
            }

            return new OkObjectResult(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", documentId);
            return new ObjectResult(new { error = "Failed to get document" }) { StatusCode = 500 };
        }
    }

    [Function("DeleteDocument")]
    public async Task<IActionResult> DeleteDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "management/documents/{documentId}")] HttpRequest req,
        string documentId)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var document = await _cosmosDbService.GetDocumentByIdAsync(documentId);
            if (document == null)
            {
                return new NotFoundObjectResult(new { error = "Document not found" });
            }

            // Delete from AI Search index
            await _searchService.DeleteDocumentChunksAsync(documentId);

            // Delete blob from storage
            var blobPath = ResolveBlobPath(document);
            await _blobStorageService.DeleteBlobAsync(blobPath);

            // Delete document record
            await _cosmosDbService.DeleteDocumentAsync(documentId);

            _logger.LogInformation("Document deleted: {DocumentId}", documentId);

            return new OkObjectResult(new { success = true, message = "Document deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return new ObjectResult(new { error = "Failed to delete document" }) { StatusCode = 500 };
        }
    }

    [Function("DownloadDocument")]
    public async Task<IActionResult> DownloadDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "management/documents/{documentId}/download")] HttpRequest req,
        string documentId)
    {
        try
        {
            var errorResult = await _authService.ValidateAdminRequestAsync(req);
            if (errorResult != null)
            {
                return errorResult;
            }

            var document = await _cosmosDbService.GetDocumentByIdAsync(documentId);
            if (document == null)
            {
                return new NotFoundObjectResult(new { error = "Document not found" });
            }

            // Generate SAS URL for download (valid for 1 hour)
            var blobPath = ResolveBlobPath(document);
            var sasUrl = await _blobStorageService.GenerateSasUrlAsync(blobPath, TimeSpan.FromHours(1));

            return new OkObjectResult(new
            {
                downloadUrl = sasUrl,
                fileName = document.OriginalFileName,
                expiresIn = 3600
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for document {DocumentId}", documentId);
            return new ObjectResult(new { error = "Failed to generate download URL" }) { StatusCode = 500 };
        }
    }
}

using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using AzureFunctionApp.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Functions;

public class DocumentUploadFunction
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentParserService _documentParserService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<DocumentUploadFunction> _logger;

    public DocumentUploadFunction(
        IBlobStorageService blobStorageService,
        IDocumentParserService documentParserService,
        IAuthenticationService authenticationService,
        ILogger<DocumentUploadFunction> logger)
    {
        _blobStorageService = blobStorageService;
        _documentParserService = documentParserService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [Function("DocumentUpload")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document/upload")]
        HttpRequest req)
    {
        _logger.LogInformation("Document upload request received");

        try
        {
            // Authenticate user
            if (!_authenticationService.ValidateRequestToken(req, out string userId, out _, out _))
            {
                return new UnauthorizedObjectResult(new { error = "Unauthorized" });
            }

            // Get conversation ID from query
            var conversationId = req.Query["conversationId"].ToString();

            // Check if request has multipart content
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult(new { error = "Request must be multipart/form-data" });
            }

            var form = await req.ReadFormAsync();
            var files = form.Files;

            if (files.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "No documents provided" });
            }

            // Validate number of files
            if (files.Count > DocumentValidator.MaxDocumentsPerMessage)
            {
                return new BadRequestObjectResult(new { error = $"Maximum {DocumentValidator.MaxDocumentsPerMessage} documents allowed per upload" });
            }

            var uploadedDocuments = new List<DocumentAttachment>();
            var uploadedBlobs = new List<string>();

            try
            {
                foreach (var file in files)
                {
                    // Validate document
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!new[] { ".pdf", ".txt", ".doc", ".docx" }.Contains(extension))
                    {
                        return new BadRequestObjectResult(new { error = $"Unsupported document type: {extension}. Allowed: PDF, TXT, DOC, DOCX" });
                    }

                    if (file.Length > DocumentValidator.MaxDocumentSizeBytes)
                    {
                        return new BadRequestObjectResult(new { error = $"Document {file.FileName} exceeds maximum size of 10MB" });
                    }

                    var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Extract text based on file type
                    string extractedText;
                    int? pageCount = null;

                    try
                    {
                        if (extension == ".pdf")
                        {
                            extractedText = await _documentParserService.ExtractTextFromPdfAsync(memoryStream);
                            memoryStream.Position = 0;
                            pageCount = _documentParserService.CountPages(memoryStream);
                            memoryStream.Position = 0;
                        }
                        else if (extension == ".docx" || extension == ".doc")
                        {
                            extractedText = await _documentParserService.ExtractTextFromWordAsync(memoryStream);
                        }
                        else // .txt
                        {
                            extractedText = await _documentParserService.ExtractTextFromTxtAsync(memoryStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract text from document: {FileName}", file.FileName);
                        return new BadRequestObjectResult(new { error = $"Failed to extract text from {file.FileName}" });
                    }

                    // Upload to blob storage. Derive the content type server-side from the
                    // already-allowlisted extension instead of trusting the client-supplied
                    // ContentType header (which is attacker-controlled).
                    memoryStream.Position = 0;
                    var contentType = GetContentTypeForExtension(extension);
                    var document = await _blobStorageService.UploadDocumentAsync(
                        memoryStream,
                        file.FileName,
                        contentType,
                        conversationId);

                    // Add extracted text and word count
                    document.ExtractedText = extractedText;
                    document.WordCount = CountWords(extractedText);
                    document.PageCount = pageCount;

                    uploadedDocuments.Add(document);
                    uploadedBlobs.Add(document.BlobName);

                    _logger.LogInformation(
                        "Uploaded document: {FileName}, Size: {Size}, Pages: {Pages}, Words: {Words}",
                        file.FileName,
                        file.Length,
                        pageCount,
                        document.WordCount);
                }

                // Return success response
                return new OkObjectResult(new
                {
                    message = $"Successfully uploaded {uploadedDocuments.Count} document(s)",
                    documents = uploadedDocuments
                });
            }
            catch (Exception ex)
            {
                // Cleanup: delete any successfully uploaded blobs
                _logger.LogError(ex, "Error during document upload, cleaning up");
                foreach (var blobName in uploadedBlobs)
                {
                    try
                    {
                        await _blobStorageService.DeleteDocumentAsync(blobName);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup blob: {BlobName}", blobName);
                    }
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading documents");
            return new ObjectResult(new { error = "Error uploading documents" })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Maps an allowlisted file extension to a trusted, server-determined content type.
    /// The extension is validated against the allowlist before this is called, so the
    /// default branch should never be reached in practice.
    /// </summary>
    private static string GetContentTypeForExtension(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

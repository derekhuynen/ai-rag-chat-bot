using Microsoft.AspNetCore.Http;

namespace AzureFunctionApp.Utils;

public static class DocumentValidator
{
    public const long MaxDocumentSizeBytes = 10 * 1024 * 1024; // 10MB
    public const int MaxDocumentsPerMessage = 5;
    public const int MaxExtractedTextLength = 100_000;

    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "application/pdf",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly HashSet<string> AllowedExtensions = new()
    {
        ".pdf", ".txt", ".doc", ".docx"
    };

    public static (bool isValid, string? errorMessage) ValidateDocuments(IFormFileCollection files)
    {
        if (files.Count == 0)
        {
            return (false, "No documents provided");
        }

        if (files.Count > MaxDocumentsPerMessage)
        {
            return (false, $"Maximum {MaxDocumentsPerMessage} documents allowed per message");
        }

        foreach (var file in files)
        {
            var validation = ValidateDocument(file);
            if (!validation.isValid)
            {
                return validation;
            }
        }

        return (true, null);
    }

    public static (bool isValid, string? errorMessage) ValidateDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Empty document");
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            return (false, $"Document {file.FileName} exceeds maximum size of 10MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (false, $"Document type {extension} not supported. Allowed types: PDF, TXT, DOC, DOCX");
        }

        if (!string.IsNullOrEmpty(file.ContentType) && !AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return (false, $"Invalid document MIME type: {file.ContentType}");
        }

        return (true, null);
    }

    public static (bool isValid, string? errorMessage) ValidateUploadedDocument(Stream stream, string fileName, string contentType)
    {
        if (stream == null || stream.Length == 0)
        {
            return (false, "Empty document stream");
        }

        if (stream.Length > MaxDocumentSizeBytes)
        {
            return (false, $"Document exceeds maximum size of 10MB");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (false, $"Document type {extension} not supported");
        }

        return (true, null);
    }
}

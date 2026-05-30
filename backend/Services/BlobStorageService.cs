using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureFunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureFunctionApp.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _imageFolder;
    private readonly string _documentFolder;

    public BlobStorageService(IConfiguration configuration, TokenCredential credential, ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        var accountName = configuration["AzureStorage:AccountName"]
            ?? throw new InvalidOperationException("Azure Storage account name not configured");
        var containerName = configuration["AzureStorage:ContainerName"] ?? "ai-chat";
        _imageFolder = configuration["AzureStorage:ImageFolder"] ?? "images";
        _documentFolder = configuration["AzureStorage:DocumentFolder"] ?? "documents";

        var blobServiceUri = new Uri($"https://{accountName}.blob.core.windows.net");
        _blobServiceClient = new BlobServiceClient(blobServiceUri, credential);
        _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        _logger.LogInformation("BlobStorageService initialized with container: {Container}", containerName);
    }

    public async Task<ImageAttachment> UploadImageAsync(
        Stream imageStream,
        string filename,
        string mimeType,
        string? conversationId = null)
    {
        try
        {
            // Generate unique blob name: ai-chat/images/{conversationId}/{timestamp}_{filename}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sanitizedFilename = SanitizeFilename(filename);
            var blobPath = conversationId != null
                ? $"{_imageFolder}/{conversationId}/{timestamp}_{sanitizedFilename}"
                : $"{_imageFolder}/temp/{timestamp}_{sanitizedFilename}";

            var blobClient = _containerClient.GetBlobClient(blobPath);

            // Upload with metadata
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = mimeType
            };

            await blobClient.UploadAsync(imageStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded image to blob: {BlobName}", blobPath);

            // Get URL with SAS token
            var url = await GetImageUrlWithSasAsync(blobPath, expiryMinutes: 10080); // user-delegation SAS max lifetime is 7 days

            return new ImageAttachment
            {
                Id = Guid.NewGuid().ToString(),
                Url = url,
                MimeType = mimeType,
                Filename = filename,
                Size = imageStream.Length,
                BlobName = blobPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to blob storage");
            throw;
        }
    }

    public async Task DeleteImageAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Deleted image blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from blob storage: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<string> GetImageUrlWithSasAsync(string blobName, int expiryMinutes = 60)
    {
        try
        {
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
            return await BuildUserDelegationSasUrlAsync(blobName, expiresOn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URL for blob: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<bool> BlobExistsAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if blob exists: {BlobName}", blobName);
            return false;
        }
    }

    public async Task<DocumentAttachment> UploadDocumentAsync(
        Stream documentStream,
        string filename,
        string mimeType,
        string? conversationId = null)
    {
        try
        {
            // Generate unique blob name: documents/{conversationId}/{timestamp}_{filename}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sanitizedFilename = SanitizeFilename(filename);
            var blobPath = conversationId != null
                ? $"{_documentFolder}/{conversationId}/{timestamp}_{sanitizedFilename}"
                : $"{_documentFolder}/temp/{timestamp}_{sanitizedFilename}";

            var blobClient = _containerClient.GetBlobClient(blobPath);

            // Upload with metadata
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = mimeType
            };

            await blobClient.UploadAsync(documentStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded document to blob: {BlobName}", blobPath);

            // Get URL with SAS token
            var url = await GetImageUrlWithSasAsync(blobPath, expiryMinutes: 10080); // user-delegation SAS max lifetime is 7 days

            return new DocumentAttachment
            {
                Id = Guid.NewGuid().ToString(),
                Url = url,
                MimeType = mimeType,
                Filename = filename,
                Size = documentStream.Length,
                BlobName = blobPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to blob storage");
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Deleted document blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document from blob storage: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<string> UploadBlobAsync(Stream stream, string blobPath, string mimeType)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = mimeType });
            
            _logger.LogInformation("Uploaded blob: {BlobPath}", blobPath);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blob: {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task<string> DownloadBlobTextAsync(string blobPath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            using var stream = await blobClient.OpenReadAsync();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob text: {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string blobPath)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Deleted blob: {BlobPath}", blobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob: {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task<string> GenerateSasUrlAsync(string blobPath, TimeSpan expiry)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobPath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob not found: {blobPath}");
            }

            var expiresOn = DateTimeOffset.UtcNow.Add(expiry);
            return await BuildUserDelegationSasUrlAsync(blobPath, expiresOn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URL for blob: {BlobPath}", blobPath);
            throw;
        }
    }

    private async Task<string> BuildUserDelegationSasUrlAsync(string blobName, DateTimeOffset expiresOn)
    {
        var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);

        // A user-delegation key (and any SAS it signs) is valid at most 7 days from its start,
        // so clamp the window. The 5-minute clock-skew start means the cap is start + 7 days.
        var maxExpiry = startsOn.AddDays(7);
        if (expiresOn > maxExpiry)
        {
            expiresOn = maxExpiry;
        }

        // User-delegation key is obtained via the managed identity (no account key needed).
        var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn);

        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = startsOn,
            ExpiresOn = expiresOn
        };
        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

        var blobClient = _containerClient.GetBlobClient(blobName);
        var sasParams = sasBuilder.ToSasQueryParameters(
            userDelegationKey.Value,
            _blobServiceClient.AccountName);

        var uriBuilder = new Azure.Storage.Blobs.BlobUriBuilder(blobClient.Uri)
        {
            Sas = sasParams
        };
        return uriBuilder.ToUri().ToString();
    }

    private static string SanitizeFilename(string filename)
    {
        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }
}

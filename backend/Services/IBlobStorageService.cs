using AzureFunctionApp.Models;

namespace AzureFunctionApp.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads an image to blob storage
    /// </summary>
    /// <param name="imageStream">Stream of the image data</param>
    /// <param name="filename">Original filename</param>
    /// <param name="mimeType">MIME type of the image</param>
    /// <param name="conversationId">Optional conversation ID to organize images</param>
    /// <returns>ImageAttachment with URL and metadata</returns>
    Task<ImageAttachment> UploadImageAsync(Stream imageStream, string filename, string mimeType, string? conversationId = null);

    /// <summary>
    /// Deletes an image from blob storage
    /// </summary>
    /// <param name="blobName">Name of the blob to delete</param>
    Task DeleteImageAsync(string blobName);

    /// <summary>
    /// Gets a SAS URL for an image with read permissions
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="expiryMinutes">How long the SAS token should be valid (default 60 minutes)</param>
    /// <returns>URL with SAS token</returns>
    Task<string> GetImageUrlWithSasAsync(string blobName, int expiryMinutes = 60);

    /// <summary>
    /// Checks if a blob exists
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <returns>True if blob exists</returns>
    Task<bool> BlobExistsAsync(string blobName);

    /// <summary>
    /// Uploads a document to blob storage
    /// </summary>
    /// <param name="documentStream">Stream of the document data</param>
    /// <param name="filename">Original filename</param>
    /// <param name="mimeType">MIME type of the document</param>
    /// <param name="conversationId">Optional conversation ID to organize documents</param>
    /// <returns>DocumentAttachment with URL and metadata</returns>
    Task<DocumentAttachment> UploadDocumentAsync(Stream documentStream, string filename, string mimeType, string? conversationId = null);

    /// <summary>
    /// Deletes a document from blob storage
    /// </summary>
    /// <param name="blobName">Name of the blob to delete</param>
    Task DeleteDocumentAsync(string blobName);

    /// <summary>
    /// Uploads a blob to storage
    /// </summary>
    Task<string> UploadBlobAsync(Stream stream, string blobPath, string mimeType);

    /// <summary>
    /// Deletes a blob from storage
    /// </summary>
    Task DeleteBlobAsync(string blobPath);

    /// <summary>
    /// Generates a SAS URL for a blob
    /// </summary>
    Task<string> GenerateSasUrlAsync(string blobPath, TimeSpan expiry);

    Task<string> DownloadBlobTextAsync(string blobPath);
}

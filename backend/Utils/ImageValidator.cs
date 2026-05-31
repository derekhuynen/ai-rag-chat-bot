using AzureFunctionApp.Models;

namespace AzureFunctionApp.Utils;

public static class ImageValidator
{
    private const long MaxImageSizeBytes = 20 * 1024 * 1024; // 20MB
    private const int MaxImagesPerMessage = 10;

    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    private static readonly HashSet<string> AllowedExtensions = new()
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
        ".bmp"
    };

    /// <summary>
    /// Validates a list of image attachments
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateImages(List<ImageAttachment>? images)
    {
        if (images == null || images.Count == 0)
        {
            return (true, null);
        }

        if (images.Count > MaxImagesPerMessage)
        {
            return (false, $"Maximum {MaxImagesPerMessage} images allowed per message");
        }

        foreach (var image in images)
        {
            var validation = ValidateImage(image);
            if (!validation.IsValid)
            {
                return validation;
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a single image attachment
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateImage(ImageAttachment image)
    {
        // Validate MIME type
        if (string.IsNullOrWhiteSpace(image.MimeType) || !AllowedMimeTypes.Contains(image.MimeType.ToLower()))
        {
            return (false, $"Invalid image type: {image.MimeType}. Allowed types: {string.Join(", ", AllowedMimeTypes)}");
        }

        // Validate size
        if (image.Size <= 0)
        {
            return (false, "Image size must be greater than 0");
        }

        if (image.Size > MaxImageSizeBytes)
        {
            var sizeMB = image.Size / (1024.0 * 1024.0);
            return (false, $"Image too large: {sizeMB:F2}MB (max 20MB)");
        }

        // Validate filename
        if (string.IsNullOrWhiteSpace(image.Filename))
        {
            return (false, "Image filename is required");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates uploaded file stream and metadata
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateUploadedFile(
        Stream fileStream,
        string filename,
        string contentType,
        long fileSize)
    {
        // Validate stream
        if (fileStream == null || !fileStream.CanRead)
        {
            return (false, "Invalid file stream");
        }

        // Validate filename
        if (string.IsNullOrWhiteSpace(filename))
        {
            return (false, "Filename is required");
        }

        var extension = Path.GetExtension(filename).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            return (false, $"Invalid file extension: {extension}. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        // Validate MIME type
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedMimeTypes.Contains(contentType.ToLower()))
        {
            return (false, $"Invalid content type: {contentType}. Allowed types: {string.Join(", ", AllowedMimeTypes)}");
        }

        // Validate size
        if (fileSize <= 0)
        {
            return (false, "File size must be greater than 0");
        }

        if (fileSize > MaxImageSizeBytes)
        {
            var sizeMB = fileSize / (1024.0 * 1024.0);
            return (false, $"File too large: {sizeMB:F2}MB (max 20MB)");
        }

        return (true, null);
    }

    public static long MaxImageSize => MaxImageSizeBytes;
    public static int MaxImages => MaxImagesPerMessage;
}

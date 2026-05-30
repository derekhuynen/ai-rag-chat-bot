using AzureFunctionApp.Models;
using AzureFunctionApp.Services;
using AzureFunctionApp.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AzureFunctionApp.Functions;

public class ImageUploadFunction
{
    private readonly ILogger<ImageUploadFunction> _logger;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IAuthenticationService _authenticationService;

    public ImageUploadFunction(
        ILogger<ImageUploadFunction> logger,
        IBlobStorageService blobStorageService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
        _authenticationService = authenticationService;
    }

    [Function("ImageUpload")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "image/upload")] HttpRequest req)
    {
        _logger.LogInformation("Image upload request received");

        try
        {
            // Validate authentication
            if (!_authenticationService.ValidateRequestToken(req, out string userId, out _, out _))
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            }

            // Get conversation ID from query or form
            string? conversationId = req.Query["conversationId"];

            // Check if request has multipart content
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult(new { error = "Request must be multipart/form-data" });
            }

            var form = await req.ReadFormAsync();
            var files = form.Files;

            if (files.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "No files uploaded" });
            }

            // Validate number of files
            if (files.Count > 10)
            {
                return new BadRequestObjectResult(new { error = "Maximum 10 images allowed per upload" });
            }

            var uploadedImages = new List<ImageAttachment>();

            foreach (var file in files)
            {
                // Validate file
                var validation = ImageValidator.ValidateUploadedFile(
                    file.OpenReadStream(),
                    file.FileName,
                    file.ContentType,
                    file.Length);

                if (!validation.IsValid)
                {
                    return new BadRequestObjectResult(new { error = validation.ErrorMessage });
                }

                try
                {
                    // Upload to blob storage
                    using var stream = file.OpenReadStream();
                    var imageAttachment = await _blobStorageService.UploadImageAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        conversationId);

                    uploadedImages.Add(imageAttachment);

                    _logger.LogInformation("Uploaded image: {Filename} to {BlobName}",
                        file.FileName, imageAttachment.BlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file: {Filename}", file.FileName);

                    // Clean up already uploaded images on failure
                    foreach (var img in uploadedImages)
                    {
                        try
                        {
                            await _blobStorageService.DeleteImageAsync(img.BlobName);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogError(deleteEx, "Error cleaning up blob: {BlobName}", img.BlobName);
                        }
                    }

                    return new ObjectResult(new { error = "Failed to upload images" })
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }
            }

            return new OkObjectResult(new
            {
                message = $"Successfully uploaded {uploadedImages.Count} image(s)",
                images = uploadedImages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in image upload function");
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}

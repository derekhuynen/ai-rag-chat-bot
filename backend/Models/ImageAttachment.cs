using Newtonsoft.Json;

namespace AzureFunctionApp.Models;

public class ImageAttachment
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonProperty("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("blobName")]
    public string BlobName { get; set; } = string.Empty;

    [JsonProperty("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }
}

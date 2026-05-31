using Newtonsoft.Json;

namespace AzureFunctionApp.Models;

public class DocumentAttachment
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

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

    [JsonProperty("extractedText")]
    public string ExtractedText { get; set; } = string.Empty;

    [JsonProperty("pageCount")]
    public int? PageCount { get; set; }

    [JsonProperty("wordCount")]
    public int WordCount { get; set; }
}

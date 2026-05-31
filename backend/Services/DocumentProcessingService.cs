using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Azure;
using Azure.Core;
using System.Text;
using OpenAI.Embeddings;
using OpenAI.Chat;

namespace AzureFunctionApp.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly string _deploymentName;
    private readonly string _embeddingDeployment;
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public DocumentProcessingService(
        IConfiguration configuration,
        TokenCredential credential,
        ILogger<DocumentProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var endpoint = configuration["AzureAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureAI:Endpoint not configured");

        _deploymentName = configuration["AzureAI:DeploymentName"] ?? "gpt-4";
        _embeddingDeployment = configuration["AzureAI:EmbeddingDeployment"] ?? "text-embedding-3-small";
        _chunkSize = int.Parse(configuration["DocumentProcessing:ChunkSize"] ?? "800");
        _chunkOverlap = int.Parse(configuration["DocumentProcessing:ChunkOverlap"] ?? "200");

        _openAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
    }

    public async Task<List<DocumentChunk>> ChunkDocumentAsync(string content, string documentId, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting chunking for document {DocumentId}", documentId);

            var chunks = new List<DocumentChunk>();

            // Split content into sentences for better chunking
            var sentences = SplitIntoSentences(content);

            var currentChunk = new StringBuilder();
            var currentTokenCount = 0;
            var chunkIndex = 0;

            for (int i = 0; i < sentences.Length; i++)
            {
                var sentence = sentences[i];
                var sentenceTokenCount = EstimateTokenCount(sentence);

                // If adding this sentence would exceed chunk size and we have content
                if (currentTokenCount + sentenceTokenCount > _chunkSize && currentChunk.Length > 0)
                {
                    // Create chunk
                    var chunkText = currentChunk.ToString().Trim();
                    chunks.Add(new DocumentChunk
                    {
                        Id = $"{documentId}_chunk{chunkIndex}",
                        DocumentId = documentId,
                        FileName = fileName,
                        Content = chunkText,
                        ChunkIndex = chunkIndex,
                        Page = 1 // .txt files are single page
                    });

                    _logger.LogDebug("Created chunk {ChunkIndex} with {TokenCount} tokens", chunkIndex, currentTokenCount);

                    // Calculate overlap: keep last N tokens worth of sentences
                    var overlapText = GetOverlapText(currentChunk.ToString(), _chunkOverlap);
                    currentChunk.Clear();
                    currentChunk.Append(overlapText);
                    currentTokenCount = EstimateTokenCount(overlapText);

                    chunkIndex++;
                }

                currentChunk.Append(sentence).Append(" ");
                currentTokenCount += sentenceTokenCount;
            }

            // Add final chunk if there's remaining content
            if (currentChunk.Length > 0)
            {
                var chunkText = currentChunk.ToString().Trim();
                chunks.Add(new DocumentChunk
                {
                    Id = $"{documentId}_chunk{chunkIndex}",
                    DocumentId = documentId,
                    FileName = fileName,
                    Content = chunkText,
                    ChunkIndex = chunkIndex,
                    Page = 1
                });
            }

            _logger.LogInformation("Document {DocumentId} chunked into {ChunkCount} chunks", documentId, chunks.Count);
            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<string> SummarizeChunkAsync(string chunkContent)
    {
        try
        {
            var prompt = $@"Summarize the following text in simple, clear terms. 
Focus on the key facts, concepts, and actionable information.
Keep the summary under 200 words.

Text:
{chunkContent}

Summary:";

            var chatClient = _openAIClient.GetChatClient(_deploymentName);
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new OpenAI.Chat.SystemChatMessage("You are a helpful assistant that creates concise summaries."),
                new OpenAI.Chat.UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var summary = response.Value.Content[0].Text;

            _logger.LogDebug("Generated summary: {Length} characters", summary.Length);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing chunk");
            // Return original content if summarization fails
            return chunkContent.Length > 500 ? chunkContent.Substring(0, 500) + "..." : chunkContent;
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var embeddingClient = _openAIClient.GetEmbeddingClient(_embeddingDeployment);
            var response = await embeddingClient.GenerateEmbeddingAsync(text);

            var embedding = response.Value.ToFloats().ToArray();
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    private string[] SplitIntoSentences(string text)
    {
        // Split on sentence boundaries while preserving the delimiter
        var sentences = new List<string>();
        var currentSentence = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            currentSentence.Append(text[i]);

            // Check for sentence ending
            if ((text[i] == '.' || text[i] == '!' || text[i] == '?') &&
                (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1])))
            {
                sentences.Add(currentSentence.ToString());
                currentSentence.Clear();
            }
        }

        // Add remaining text
        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString());
        }

        return sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    private int EstimateTokenCount(string text)
    {
        // Rough estimation: 1 token ≈ 4 characters
        // This is a simplification; actual tokenization is more complex
        return text.Length / 4;
    }

    private string GetOverlapText(string text, int overlapTokens)
    {
        // Get approximately last N tokens worth of text
        var overlapChars = overlapTokens * 4;

        if (text.Length <= overlapChars)
        {
            return text;
        }

        // Try to break at sentence boundary
        var overlapText = text.Substring(text.Length - overlapChars);
        var firstPeriod = overlapText.IndexOfAny(new[] { '.', '!', '?' });

        if (firstPeriod > 0 && firstPeriod < overlapText.Length - 1)
        {
            return overlapText.Substring(firstPeriod + 1).Trim();
        }

        return overlapText.Trim();
    }
}

using AzureFunctionApp.Utils;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AzureFunctionApp.Tests;

public class DocumentValidatorTests
{
    private static IFormFile BuildFile(string fileName, long length, string? contentType = null)
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.FileName).Returns(fileName);
        file.SetupGet(f => f.Length).Returns(length);
        file.SetupGet(f => f.ContentType).Returns(contentType ?? string.Empty);
        return file.Object;
    }

    [Theory]
    [InlineData("report.pdf")]
    [InlineData("notes.txt")]
    [InlineData("legacy.doc")]
    [InlineData("modern.docx")]
    [InlineData("UPPER.PDF")] // extension comparison is case-insensitive
    public void ValidateDocument_AllowedExtension_IsValid(string fileName)
    {
        var file = BuildFile(fileName, 1024);

        var (isValid, error) = DocumentValidator.ValidateDocument(file);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("sheet.xlsx")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    public void ValidateDocument_DisallowedExtension_IsInvalid(string fileName)
    {
        var file = BuildFile(fileName, 1024);

        var (isValid, error) = DocumentValidator.ValidateDocument(file);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateDocument_EmptyFile_IsInvalid()
    {
        var file = BuildFile("empty.pdf", 0);

        var (isValid, error) = DocumentValidator.ValidateDocument(file);

        Assert.False(isValid);
        Assert.Equal("Empty document", error);
    }

    [Fact]
    public void ValidateDocument_AtSizeLimit_IsValid()
    {
        var file = BuildFile("big.pdf", DocumentValidator.MaxDocumentSizeBytes);

        var (isValid, _) = DocumentValidator.ValidateDocument(file);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateDocument_OverSizeLimit_IsInvalid()
    {
        var file = BuildFile("big.pdf", DocumentValidator.MaxDocumentSizeBytes + 1);

        var (isValid, error) = DocumentValidator.ValidateDocument(file);

        Assert.False(isValid);
        Assert.Contains("exceeds maximum size", error);
    }

    [Fact]
    public void ValidateDocument_AllowedExtensionButWrongMimeType_IsInvalid()
    {
        var file = BuildFile("report.pdf", 1024, contentType: "application/x-evil");

        var (isValid, error) = DocumentValidator.ValidateDocument(file);

        Assert.False(isValid);
        Assert.Contains("Invalid document MIME type", error);
    }

    [Fact]
    public void ValidateDocuments_EmptyCollection_IsInvalid()
    {
        var collection = new FormFileCollection();

        var (isValid, error) = DocumentValidator.ValidateDocuments(collection);

        Assert.False(isValid);
        Assert.Equal("No documents provided", error);
    }

    [Fact]
    public void ValidateDocuments_TooManyDocuments_IsInvalid()
    {
        var collection = new FormFileCollection();
        for (var i = 0; i < DocumentValidator.MaxDocumentsPerMessage + 1; i++)
        {
            collection.Add(BuildFile($"doc{i}.pdf", 1024));
        }

        var (isValid, error) = DocumentValidator.ValidateDocuments(collection);

        Assert.False(isValid);
        Assert.Contains("Maximum", error);
    }

    [Fact]
    public void ValidateDocuments_AtMaxCount_IsValid()
    {
        var collection = new FormFileCollection();
        for (var i = 0; i < DocumentValidator.MaxDocumentsPerMessage; i++)
        {
            collection.Add(BuildFile($"doc{i}.pdf", 1024));
        }

        var (isValid, _) = DocumentValidator.ValidateDocuments(collection);

        Assert.True(isValid);
    }
}

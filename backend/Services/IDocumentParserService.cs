namespace AzureFunctionApp.Services;

public interface IDocumentParserService
{
    Task<string> ExtractTextFromPdfAsync(Stream stream);
    Task<string> ExtractTextFromWordAsync(Stream stream);
    Task<string> ExtractTextFromTxtAsync(Stream stream);
    int CountPages(Stream stream);
}

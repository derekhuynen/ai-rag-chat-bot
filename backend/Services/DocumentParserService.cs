using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using System.Text;

namespace AzureFunctionApp.Services;

public class DocumentParserService : IDocumentParserService
{
    private const int MaxTextLength = 100_000;

    public async Task<string> ExtractTextFromPdfAsync(Stream stream)
    {
        try
        {
            stream.Position = 0;
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(stream))
            {
                foreach (var page in document.GetPages())
                {
                    var text = page.Text;
                    sb.AppendLine(text);

                    if (sb.Length > MaxTextLength)
                    {
                        break;
                    }
                }
            }

            var result = sb.ToString();
            if (result.Length > MaxTextLength)
            {
                result = result.Substring(0, MaxTextLength) + "\n\n[Text truncated due to length]";
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to extract text from PDF: {ex.Message}", ex);
        }
    }

    public async Task<string> ExtractTextFromWordAsync(Stream stream)
    {
        try
        {
            stream.Position = 0;
            var sb = new StringBuilder();

            using (var document = WordprocessingDocument.Open(stream, false))
            {
                var body = document.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    foreach (var element in body.Elements())
                    {
                        if (element is Paragraph paragraph)
                        {
                            sb.AppendLine(paragraph.InnerText);
                        }
                        else if (element is Table table)
                        {
                            foreach (var row in table.Elements<TableRow>())
                            {
                                var rowText = string.Join(" | ", row.Elements<TableCell>().Select(c => c.InnerText));
                                sb.AppendLine(rowText);
                            }
                        }

                        if (sb.Length > MaxTextLength)
                        {
                            break;
                        }
                    }
                }
            }

            var result = sb.ToString();
            if (result.Length > MaxTextLength)
            {
                result = result.Substring(0, MaxTextLength) + "\n\n[Text truncated due to length]";
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to extract text from Word document: {ex.Message}", ex);
        }
    }

    public async Task<string> ExtractTextFromTxtAsync(Stream stream)
    {
        try
        {
            stream.Position = 0;
            // Don't use 'using' to avoid disposing the stream
            var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var text = await reader.ReadToEndAsync();

            if (text.Length > MaxTextLength)
            {
                text = text.Substring(0, MaxTextLength) + "\n\n[Text truncated due to length]";
            }

            return text;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to extract text from TXT file: {ex.Message}", ex);
        }
    }

    public int CountPages(Stream stream)
    {
        try
        {
            stream.Position = 0;
            using var document = PdfDocument.Open(stream);
            return document.NumberOfPages;
        }
        catch
        {
            return 0;
        }
    }
}

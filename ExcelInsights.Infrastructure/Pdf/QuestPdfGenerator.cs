using ExcelInsights.Application.Contracts;
using ExcelInsights.Application.DTOs;
using QuestPDF.Fluent;

namespace ExcelInsights.Infrastructure.Pdf;

public class QuestPdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateAsync(AnalysisResult result)
    {
        var builder = new ReportDocumentBuilder(result);
        var document = Document.Create(builder.Compose);
        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }
}

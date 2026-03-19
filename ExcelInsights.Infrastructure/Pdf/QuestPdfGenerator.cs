using ExcelInsights.Application.Contracts;
using ExcelInsights.Application.DTOs;

namespace ExcelInsights.Infrastructure.Pdf;

/// <summary>
/// Implementación del generador de PDF usando QuestPDF.
/// STUB — Issue 1. La lógica real se implementa en Issue 6.
/// </summary>
public class QuestPdfGenerator : IPdfGenerator
{
    public Task<byte[]> GenerateAsync(AnalysisResult result)
    {
        // Issue 6: aquí irá la construcción del documento PDF
        // con QuestPDF (secciones, tablas, estadísticas, errores).
        throw new NotImplementedException("Implementar en Issue 6 con QuestPDF.");
    }
}

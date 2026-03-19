/// <summary>
/// COMMAND — GenerateReportCommand
///
/// Mismo patrón que AnalyzeExcelCommand pero para el flujo del PDF.
/// La diferencia clave está en IRequest<T>:
///   - AnalyzeExcelCommand  → IRequest<AnalysisResult>  (devuelve JSON)
///   - GenerateReportCommand → IRequest<byte[]>          (devuelve bytes del PDF)
///
/// El endpoint que usa este Command responderá con un archivo descargable
/// en lugar de un JSON, pero el patrón Command/Handler es idéntico.
/// </summary>

using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace ExcelInsights.Application.Features.GenerateReport;

/// <summary>
/// Representa la solicitud de generar un reporte PDF de un archivo Excel.
/// </summary>
public sealed class GenerateReportCommand : IRequest<byte[]>
{
    private Stream stream;

    [SetsRequiredMembers]
    public GenerateReportCommand(Stream stream, string fileName)
    {
        this.stream = stream;
        FileStream = stream;
        FileName = fileName;
    }

    /// <summary>
    /// Stream del archivo Excel. Igual que en AnalyzeExcelCommand,
    /// usamos Stream para no cargar todo en memoria.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// Nombre del archivo, se incluye en la portada del PDF generado.
    /// </summary>
    public required string FileName { get; init; }
}
/// <summary>
/// HANDLER — GenerateReportHandler
///
/// Maneja la generación del reporte PDF.
///
/// Su flujo cuando esté completo (Issues 2-6):
///   1. Parsear el Excel           → IExcelParser
///   2. Inferir tipos              → IColumnInferrer
///   3. Validar filas              → IValidationEngine
///   4. Calcular estadísticas      → lógica en Domain
///   5. Construir AnalysisResult   → mapeo interno
///   6. Generar el PDF             → IPdfGenerator  ← paso extra vs AnalyzeHandler
///   7. Devolver byte[]            → el endpoint lo sirve como archivo descargable
///
/// En Issue 1: devuelve un array vacío. El objetivo es que el endpoint
/// responda sin errores y que el cableado de MediatR esté verificado.
/// </summary>

using MediatR;
using ExcelInsights.Application.Contracts;

namespace ExcelInsights.Application.Features.GenerateReport;

/// <summary>
/// Maneja la solicitud de generación de un reporte PDF.
/// </summary>
public sealed class GenerateReportHandler : IRequestHandler<GenerateReportCommand, byte[]>
{
    // En Issues futuras se agregará IExcelParser, IColumnInferrer,
    // IValidationEngine igual que en AnalyzeExcelHandler, más IPdfGenerator.
    private readonly IPdfGenerator _pdfGenerator;

    public GenerateReportHandler(IPdfGenerator pdfGenerator)
    {
        _pdfGenerator = pdfGenerator;
    }

    /// <summary>
    /// Genera el PDF y devuelve sus bytes.
    /// El endpoint convierte esos bytes en un archivo descargable.
    /// </summary>
    public async Task<byte[]> Handle(
        GenerateReportCommand request,
        CancellationToken cancellationToken)
    {
        // -----------------------------------------------------------------
        // STUB — Issue 1
        //
        // Devuelve array vacío. En Issue 6 reemplazamos con:
        //
        //   var excelFile = await _excelParser.ParseAsync(...);
        //   ... (mismo flujo que AnalyzeExcelHandler) ...
        //   var analysisResult = MapToResult(excelFile, errors);
        //   return await _pdfGenerator.GenerateAsync(analysisResult, cancellationToken);
        // -----------------------------------------------------------------

        await Task.CompletedTask;
        return Array.Empty<byte>();
    }
}
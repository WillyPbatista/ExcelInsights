/// <summary>
/// HANDLER — AnalyzeExcelHandler
///
/// El Handler es quien sabe CÓMO responder a un Command.
/// Es el orquestador: no hace el trabajo pesado directamente,
/// sino que llama a los contratos (interfaces) en el orden correcto.
///
/// Implementa IRequestHandler<TRequest, TResponse>:
///   - TRequest = AnalyzeExcelCommand  (el mensaje que escucha)
///   - TResponse = AnalysisResult      (lo que devuelve)
///
/// MediatR conecta automáticamente este Handler con su Command
/// porque los tipos genéricos coinciden. No hay mapeo manual.
///
/// PRINCIPIO CLAVE — este Handler sigue SRP (Single Responsibility):
///   - NO parsea el Excel       → eso es IExcelParser
///   - NO infiere tipos         → eso es IColumnInferrer
///   - NO valida filas          → eso es IValidationEngine
///   - NO genera estadísticas   → eso es ColumnStats en Domain
///   Solo ORQUESTA el flujo llamando a cada pieza en orden.
///
/// EN ISSUE 1: el método Handle devuelve un resultado vacío (stub).
/// Las llamadas reales a los contratos se implementan en Issues 2-4.
/// El objetivo ahora es verificar que el cableado de MediatR funciona.
/// </summary>

using MediatR;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Application.DTOs;

namespace ExcelInsights.Application.Features.Analyze;

/// <summary>
/// Maneja la solicitud de análisis de un archivo Excel.
/// Recibe el Command, orquesta los servicios y devuelve el resultado.
/// </summary>
public sealed class AnalyzeExcelHandler : IRequestHandler<AnalyzeExcelCommand, AnalysisResult>
{
    // -------------------------------------------------------------------------
    // DEPENDENCIAS — se inyectan por constructor (Dependency Injection)
    //
    // El Handler no instancia estos servicios con "new". Le pide al DI container
    // que se los provea. Así el Handler no está acoplado a ninguna implementación
    // concreta — solo conoce los contratos (interfaces).
    //
    // Cuando el DI container construye este Handler, busca qué clase concreta
    // está registrada para cada interfaz y la inyecta automáticamente.
    // -------------------------------------------------------------------------

    private readonly IExcelParser _excelParser;
    private readonly IColumnInferrer _columnInferrer;
    private readonly IValidationEngine _validationEngine;

    public AnalyzeExcelHandler(
        IExcelParser excelParser,
        IColumnInferrer columnInferrer,
        IValidationEngine validationEngine)
    {
        _excelParser = excelParser;
        _columnInferrer = columnInferrer;
        _validationEngine = validationEngine;
    }

    /// <summary>
    /// Método principal — MediatR lo invoca automáticamente cuando
    /// alguien hace sender.Send(AnalyzeExcelCommand).
    ///
    /// Parámetros:
    ///   request            → el Command con el Stream y el FileName
    ///   cancellationToken  → permite cancelar la operación si el cliente
    ///                        cierra la conexión antes de que termine
    /// </summary>
    public async Task<AnalysisResult> Handle(
        AnalyzeExcelCommand request,
        CancellationToken cancellationToken)
    {
        // -----------------------------------------------------------------
        // STUB — Issue 1
        //
        // Por ahora devolvemos un AnalysisResult vacío para verificar
        // que el pipeline completo funciona:
        //   Endpoint → MediatR → Handler → Respuesta
        //
        // En Issue 2 reemplazamos este stub con la llamada real a _excelParser.
        // En Issue 3 agregamos _columnInferrer y _validationEngine.
        // En Issue 4 calculamos estadísticas reales.
        //
        // La lógica real se verá así (no implementar todavía):
        //
        //   var excelFile = await _excelParser.ParseAsync(
        //       request.FileStream, request.FileName, cancellationToken);
        //
        //   foreach (var column in excelFile.Columns)
        //       column.SetInferredType(_columnInferrer.Infer(column.Values));
        //
        //   var errors = excelFile.Rows
        //       .SelectMany(row => _validationEngine.Validate(row, excelFile.Columns))
        //       .ToList();
        //
        //   return MapToResult(excelFile, errors);
        // -----------------------------------------------------------------

        await Task.CompletedTask; // eliminar cuando haya lógica async real

        return new AnalysisResult
        {
            FileName = request.FileName,
            TotalRows = 0,
            ValidRows = 0,
            InvalidRows = 0,
            Columns = [],
            Errors = []
        };
    }
}
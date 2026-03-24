// =============================================================================
// AnalyzeExcelHandler.cs — Application/Features/Analyze/
// =============================================================================
//
// CAMBIOS EN ESTE COMMIT (Issue 2):
//   - Se descomenta y activa la llamada a _excelParser.ParseAsync()
//   - AnalysisResult ahora refleja datos reales: fileName, totalRows, columnas
//   - Los TODO de Issue 2 se eliminan
//   - Los TODO de Issue 3 (inferencia) y Issue 4 (validación) se mantienen
//
// QUÉ SIGUE SIENDO STUB:
//   - InferredType = "Unknown" — Issue 3 lo resolverá
//   - Errors vacío            — Issue 4 lo resolverá
//   - Stats nulas             — Issue 5 lo resolverá
// =============================================================================

using MediatR;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Application.DTOs;

namespace ExcelInsights.Application.Features.Analyze;

public class AnalyzeExcelHandler : IRequestHandler<AnalyzeExcelCommand, AnalysisResult>
{
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

    public async Task<AnalysisResult> Handle(
        AnalyzeExcelCommand command,
        CancellationToken cancellationToken)
    {
        // ── ISSUE 2 ── ACTIVO ────────────────────────────────────────────────
        // Parseamos el Excel real. El parser extrae headers, filas y columnas
        // como strings crudos sin interpretar tipos ni validar nada.
        var excelFile = await _excelParser.ParseAsync(command.FileStream, command.FileName);

        // ── ISSUE 3 ── ACTIVO ────────────────────────────────────────────────
        // Infiere el tipo de cada columna analizando sus valores por votación.

        foreach (var column in excelFile.Columns)
        {
            column.SetInferredType(_columnInferrer.Infer(column.Values));
        }

        // ── ISSUE 4 ── TODO ──────────────────────────────────────────────────
        // Validar cada fila según el tipo inferido de cada columna.
        // var errors = excelFile.Rows
        //     .SelectMany(row => _validationEngine.Validate(row, excelFile.Columns))
        //     .ToList();

        // ── ISSUE 5 ── TODO ──────────────────────────────────────────────────
        // Calcular estadísticas por columna (avg, min, max, nulls).

        // ── RESPUESTA ────────────────────────────────────────────────────────
        // Con el parser activo, fileName y totalRows son datos reales.
        // Columns muestra los nombres reales de cada columna del Excel.
        // InferredType sigue en "Unknown" hasta Issue 3.
        // Errors sigue vacío hasta Issue 4.
        return new AnalysisResult
        {
            FileName = excelFile.FileName,
            TotalRows = excelFile.TotalRows,
            ValidRows = excelFile.TotalRows,  // todos "válidos" hasta Issue 4
            InvalidRows = 0,

            // Mapeamos cada ColumnDefinition del Domain al DTO ColumnSummary.
            // Las entidades del Domain (ColumnDefinition) nunca salen de Application —
            // el cliente solo ve el DTO.
            Columns = excelFile.Columns.Select(c => new ColumnSummary
            {
                Name = c.Name,
                InferredType = c.InferredType.DataType.ToString(),  // ← lee el tipo real
                Confidence = c.InferredType.Confidence            // ← lee la confianza real
            }).ToList(),

            Errors = new List<RowErrorDto>()  // Issue 4
        };
    }
}
// AnalyzeExcelHandler.cs

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
        _excelParser      = excelParser;
        _columnInferrer   = columnInferrer;
        _validationEngine = validationEngine;
    }

    public async Task<AnalysisResult> Handle(
        AnalyzeExcelCommand command,
        CancellationToken cancellationToken)
    {

        var excelFile = await _excelParser.ParseAsync(command.FileStream, command.FileName);


        foreach (var column in excelFile.Columns)
            column.SetInferredType(_columnInferrer.Infer(column.Values));


        var errors = excelFile.Rows
            .SelectMany(row => _validationEngine.Validate(row, excelFile.Columns))
            .ToList();


        var invalidRowCount = errors.DistinctBy(e => e.RowIndex).Count();
        var validRowCount   = excelFile.TotalRows - invalidRowCount;


        return new AnalysisResult
        {
            FileName    = excelFile.FileName,
            TotalRows   = excelFile.TotalRows,
            ValidRows   = validRowCount,
            InvalidRows = invalidRowCount,

            Columns = excelFile.Columns.Select(c => new ColumnSummary
            {
                Name         = c.Name,
                InferredType = c.InferredType.DataType.ToString(),
                Confidence   = c.InferredType.Confidence
            }).ToList(),

            Errors = errors.Select(e => new RowErrorDto
            {
                RowIndex   = e.RowIndex,
                ColumnName = e.ColumnName,
                Message    = e.Message,
                Severity   = e.Severity.ToString()
            }).ToList()
        };
    }
}
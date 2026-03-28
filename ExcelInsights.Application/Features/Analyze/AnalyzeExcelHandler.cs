// AnalyzeExcelHandler.cs

using MediatR;
using ExcelInsights.Application.DTOs;
using ExcelInsights.Application.Common;

namespace ExcelInsights.Application.Features.Analyze;

public class AnalyzeExcelHandler : IRequestHandler<AnalyzeExcelCommand, AnalysisResult>
{

    private readonly ExcelAnalysisOrchestrator _orchestrator;

    public AnalyzeExcelHandler(
        ExcelAnalysisOrchestrator orchestrator)
    {
        _orchestrator     = orchestrator;
    }

    public async Task<AnalysisResult> Handle(
        AnalyzeExcelCommand command,
        CancellationToken cancellationToken)
    {
        var result  = _orchestrator.AnalyzeAsync(command.FileStream, command.FileName);
        return await result;
    }
}
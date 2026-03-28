
using MediatR;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Application.Common;

namespace ExcelInsights.Application.Features.GenerateReport;

public sealed class GenerateReportHandler : IRequestHandler<GenerateReportCommand, byte[]>
{
    private readonly IPdfGenerator _pdfGenerator;
    private readonly ExcelAnalysisOrchestrator _orchestrator;

    public GenerateReportHandler(IPdfGenerator pdfGenerator, ExcelAnalysisOrchestrator orchestrator)
    {
        _pdfGenerator = pdfGenerator;
        _orchestrator = orchestrator;
    }
    public async Task<byte[]> Handle(
        GenerateReportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.AnalyzeAsync(request.FileStream, request.FileName);

        var pdfBytes = await _pdfGenerator.GenerateAsync(result);
        return pdfBytes;
    }
}
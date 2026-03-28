
using MediatR;
using ExcelInsights.Application.Contracts;

namespace ExcelInsights.Application.Features.GenerateReport;

public sealed class GenerateReportHandler : IRequestHandler<GenerateReportCommand, byte[]>
{
    private readonly IPdfGenerator _pdfGenerator;

    public GenerateReportHandler(IPdfGenerator pdfGenerator)
    {
        _pdfGenerator = pdfGenerator;
    }
    public async Task<byte[]> Handle(
        GenerateReportCommand request,
        CancellationToken cancellationToken)
    {

        await Task.CompletedTask;
        return Array.Empty<byte>();
    }
}
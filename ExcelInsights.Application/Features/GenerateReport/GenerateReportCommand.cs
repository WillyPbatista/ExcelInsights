

using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace ExcelInsights.Application.Features.GenerateReport;

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
    public required Stream FileStream { get; init; }

    public required string FileName { get; init; }
}
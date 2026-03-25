
using MediatR;
using ExcelInsights.Application.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace ExcelInsights.Application.Features.Analyze;

public sealed class AnalyzeExcelCommand : IRequest<AnalysisResult>
{

    public required Stream FileStream { get; init; }

    public required string FileName { get; init; }

    [SetsRequiredMembers]
    public AnalyzeExcelCommand(Stream FileStream, string FileName)
    {
        this.FileStream = FileStream;
        this.FileName = FileName;
    }
}


using MediatR;
using ExcelInsights.Application.Features.Analyze;
using ExcelInsights.Application.Features.GenerateReport;

namespace ExcelInsights.Api.Endpoints;

public static class ExcelEndpoints
{

    public static IEndpointRouteBuilder MapExcelEndpoints(this IEndpointRouteBuilder app)
    {

        var group = app.MapGroup("/api/excel")
            .WithTags("Excel");

        group.MapPost("/analyze", AnalyzeAsync)
            .WithName("AnalyzeExcel")
            .WithSummary("Analiza un archivo Excel y devuelve insights en JSON")
            .DisableAntiforgery(); 

        group.MapPost("/report", GenerateReportAsync)
            .WithName("GenerateReport")
            .WithSummary("Analiza un archivo Excel y devuelve un reporte PDF")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> AnalyzeAsync(
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("Debes subir un archivo Excel válido.");

        await using var stream = file.OpenReadStream();

        var command = new AnalyzeExcelCommand(stream, file.FileName);

        var result = await sender.Send(command, cancellationToken);


        return Results.Ok(result);
    }

    private static async Task<IResult> GenerateReportAsync(
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("Debes subir un archivo Excel válido.");

        await using var stream = file.OpenReadStream();

        var command = new GenerateReportCommand(stream, file.FileName);

        var pdfBytes = await sender.Send(command, cancellationToken);

        var reportName = $"report_{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
        return Results.File(pdfBytes, "application/pdf", reportName);
    }
}
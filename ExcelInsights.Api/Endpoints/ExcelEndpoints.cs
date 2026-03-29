
using ExcelInsights.Api.Validators;
using ExcelInsights.Application.Common;
using ExcelInsights.Application.DTOs;
using ExcelInsights.Application.Features.Analyze;
using ExcelInsights.Application.Features.GenerateReport;
using MediatR;
using Microsoft.Extensions.Options;

namespace ExcelInsights.Api.Endpoints;

public static class ExcelEndpoints
{
    public static IEndpointRouteBuilder MapExcelEndpoints(this IEndpointRouteBuilder app)
    {
        // WithOpenApi() a nivel de grupo aplica la generación de docs
        // a todos los endpoints del grupo — no necesitas repetirlo en cada uno.
        var group = app.MapGroup("/api/excel")
            .WithTags("Excel")
            .WithOpenApi();

        // ── POST /api/excel/analyze ──────────────────────────────────────────
        group.MapPost("/analyze", AnalyzeAsync)
            .WithName("AnalyzeExcel")
            .WithSummary("Analiza un archivo Excel")
            .WithDescription("""
                Recibe un archivo Excel (.xlsx o .xls), detecta automáticamente
                el tipo de cada columna, valida los datos fila a fila y calcula
                estadísticas por columna.

                **Límites:**
                - Tamaño máximo: 10 MB (configurable en appsettings.json)
                - Filas máximas: 50,000 (configurable en appsettings.json)
                - Formatos aceptados: .xlsx, .xls

                **Respuesta exitosa (200):**
                JSON con el resumen del análisis, estadísticas por columna
                y lista de errores detectados con su severidad.

                **Errores posibles:**
                - 400: archivo inválido, extensión incorrecta o vacío
                - 422: archivo demasiado grande o demasiadas filas
                - 500: error interno del servidor
                """)
            // Swagger mostrará el esquema de AnalysisResult para el 200
            .Produces<AnalysisResult>(StatusCodes.Status200OK)
            // Swagger mostrará el esquema de ErrorResponse para los errores
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

        // ── POST /api/excel/report ───────────────────────────────────────────
        group.MapPost("/report", GenerateReportAsync)
            .WithName("GenerateReport")
            .WithSummary("Genera un reporte PDF del análisis")
            .WithDescription("""
                Realiza el mismo análisis que /analyze y devuelve un archivo
                PDF descargable con tres secciones:

                1. **Resumen** — totales de filas válidas e inválidas
                2. **Estadísticas por columna** — tipo inferido, confianza, promedio, min, max
                3. **Errores detectados** — fila, columna, mensaje y severidad (Error/Warning)

                El archivo se descarga con el nombre report_{nombre_original}.pdf

                **Límites:**
                - Tamaño máximo: 10 MB (configurable en appsettings.json)
                - Filas máximas: 50,000 (configurable en appsettings.json)
                - Formatos aceptados: .xlsx, .xls

                **Errores posibles:**
                - 400: archivo inválido, extensión incorrecta o vacío
                - 422: archivo demasiado grande o demasiadas filas
                - 500: error interno del servidor
                """)
            // Para el PDF indicamos el content type en lugar del tipo genérico
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

        return app;
    }

    // -------------------------------------------------------------------------
    // POST /api/excel/analyze
    // -------------------------------------------------------------------------
    //
    // IOptions<ExcelInsightsSettings> se inyecta directamente en el parámetro
    // del lambda — Minimal APIs resuelve automáticamente los servicios del DI.
    // No necesitas un constructor ni una clase para acceder a la configuración.

    private static async Task<IResult> AnalyzeAsync(
        IFormFile file,
        ISender sender,
        IOptions<ExcelInsightsSettings> settings,
        CancellationToken cancellationToken)
    {
        var validationError = UploadFileValidator.Validate(file, settings.Value);
        if (validationError is not null)
            return Results.BadRequest(new ErrorResponse(validationError));

        await using var stream = file.OpenReadStream();
        var command = new AnalyzeExcelCommand(stream, file.FileName);
        var result  = await sender.Send(command, cancellationToken);

        return Results.Ok(result);
    }

    // -------------------------------------------------------------------------
    // POST /api/excel/report
    // -------------------------------------------------------------------------

    private static async Task<IResult> GenerateReportAsync(
        IFormFile file,
        ISender sender,
        IOptions<ExcelInsightsSettings> settings,
        CancellationToken cancellationToken)
    {
        var validationError = UploadFileValidator.Validate(file, settings.Value);
        if (validationError is not null)
            return Results.BadRequest(new ErrorResponse(validationError));

        await using var stream = file.OpenReadStream();
        var command  = new GenerateReportCommand(stream, file.FileName);
        var pdfBytes = await sender.Send(command, cancellationToken);

        var reportName = $"report_{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
        return Results.File(pdfBytes, "application/pdf", reportName);
    }
}


public record ErrorResponse(string Message);
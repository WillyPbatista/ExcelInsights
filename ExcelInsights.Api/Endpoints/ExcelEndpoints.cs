// =============================================================================
// ExcelEndpoints.cs — Api/Endpoints/
// =============================================================================
//
// ¿QUÉ SON LAS MINIMAL APIs?
// En .NET, la forma clásica de definir endpoints era con Controllers:
// clases que heredan de ControllerBase con atributos [HttpPost], [Route], etc.
//
// Minimal APIs son una alternativa más directa: defines el endpoint
// como un método lambda, sin clases ni herencia. El resultado es
// menos código y más legible para APIs pequeñas o medianas.
//
// ¿POR QUÉ UN MÉTODO DE EXTENSIÓN SOBRE IEndpointRouteBuilder?
// Para que Program.cs quede limpio. Program.cs llama app.MapExcelEndpoints()
// y no sabe nada de las rutas internas. Si agregas más endpoints al grupo,
// Program.cs no cambia.
//
// ¿QUÉ ES ISender?
// Es la interfaz de MediatR que expone el método Send().
// El endpoint la recibe por inyección de dependencias directamente
// en el parámetro del lambda — Minimal APIs soporta DI así,
// sin necesidad de constructor ni clase.
//
// ¿QUÉ ES IFormFile?
// Es la abstracción de ASP.NET para archivos subidos por multipart/form-data.
// Tiene propiedades como FileName, Length, ContentType,
// y el método OpenReadStream() que devuelve el Stream del archivo.
//
// ¿POR QUÉ MapGroup?
// Agrupa endpoints bajo un prefijo de ruta común ("/api/excel").
// También permite aplicar configuraciones a todos los endpoints del grupo
// (tags de Swagger, autenticación, rate limiting) en un solo lugar.
// =============================================================================

using MediatR;
using ExcelInsights.Application.Features.Analyze;
using ExcelInsights.Application.Features.GenerateReport;

namespace ExcelInsights.Api.Endpoints;

/// <summary>
/// Define todos los endpoints relacionados con el análisis de archivos Excel.
/// Se registra en Program.cs con: app.MapExcelEndpoints()
/// </summary>
public static class ExcelEndpoints
{
    /// <summary>
    /// Registra el grupo de endpoints bajo la ruta base /api/excel.
    /// </summary>
    public static IEndpointRouteBuilder MapExcelEndpoints(this IEndpointRouteBuilder app)
    {
        // MapGroup agrupa los endpoints bajo /api/excel
        var group = app.MapGroup("/api/excel")
            .WithTags("Excel"); // agrupa en Swagger bajo la etiqueta "Excel"

        group.MapPost("/analyze", AnalyzeAsync)
            .WithName("AnalyzeExcel")
            .WithSummary("Analiza un archivo Excel y devuelve insights en JSON")
            .DisableAntiforgery(); // necesario para multipart/form-data en Minimal APIs

        group.MapPost("/report", GenerateReportAsync)
            .WithName("GenerateReport")
            .WithSummary("Analiza un archivo Excel y devuelve un reporte PDF")
            .DisableAntiforgery();

        return app;
    }

    // -------------------------------------------------------------------------
    // ENDPOINT: POST /api/excel/analyze
    // -------------------------------------------------------------------------
    //
    // ¿POR QUÉ static async Task<IResult>?
    //   - static: no necesita instancia de clase, es un método puro.
    //   - async Task: porque el Handler usa await internamente.
    //   - IResult: interfaz de Minimal APIs para respuestas
    //     (Results.Ok, Results.BadRequest, Results.File, etc.)
    //
    // ¿POR QUÉ LOS PARÁMETROS SE INYECTAN ASÍ?
    //   Minimal APIs resuelve automáticamente los parámetros del lambda:
    //   - IFormFile file    → viene del form-data de la request
    //   - ISender sender    → viene del DI container (inyección automática)
    //   - CancellationToken → viene del runtime de ASP.NET (cancelación)
    //   No necesitas ningún atributo especial para que esto funcione.
    // -------------------------------------------------------------------------

    private static async Task<IResult> AnalyzeAsync(
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Validación básica antes de procesar.
        if (file is null || file.Length == 0)
            return Results.BadRequest("Debes subir un archivo Excel válido.");

        // Abre el stream del archivo.
        // OpenReadStream() no carga el archivo completo en memoria:
        // es un stream de lectura que el parser consumirá progresivamente.
        await using var stream = file.OpenReadStream();

        // Construye el Command con los datos de la request.
        // El Command es inmutable — se crea con todos sus datos de una vez.
        var command = new AnalyzeExcelCommand(stream, file.FileName);

        // Lanza el Command a MediatR. Internamente:
        //   1. Busca el Handler registrado para AnalyzeExcelCommand
        //   2. Construye AnalyzeExcelHandler inyectando sus dependencias
        //   3. Llama a Handle(command, cancellationToken)
        //   4. Devuelve el AnalysisResult
        var result = await sender.Send(command, cancellationToken);

        // Results.Ok() serializa el objeto a JSON automáticamente
        // y responde con HTTP 200 y Content-Type: application/json
        return Results.Ok(result);
    }

    // -------------------------------------------------------------------------
    // ENDPOINT: POST /api/excel/report
    // -------------------------------------------------------------------------

    private static async Task<IResult> GenerateReportAsync(
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("Debes subir un archivo Excel válido.");

        await using var stream = file.OpenReadStream();

        var command = new GenerateReportCommand(stream, file.FileName);

        // El handler devuelve byte[] — los bytes del PDF generado.
        var pdfBytes = await sender.Send(command, cancellationToken);

        // Results.File() devuelve el archivo al cliente con:
        //   - Content-Type: application/pdf
        //   - Content-Disposition: attachment (el browser lo descarga)
        //   - Nombre sugerido para el archivo descargado
        var reportName = $"report_{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
        return Results.File(pdfBytes, "application/pdf", reportName);
    }
}
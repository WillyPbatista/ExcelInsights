// =============================================================================
// GlobalExceptionMiddleware.cs — Api/Middlewares/
// =============================================================================
//
// ¿QUÉ ES UN MIDDLEWARE EN ASP.NET CORE?
// La pipeline de ASP.NET es una cadena de middlewares. Cada request
// la recorre de arriba hacia abajo, y cada middleware decide si pasa
// al siguiente o corta la cadena.
//
// Visualmente:
//   Request entrante
//       ↓
//   GlobalExceptionMiddleware  ← envuelve todo en try/catch
//       ↓
//   Routing
//       ↓
//   Endpoint (ExcelEndpoints)
//       ↓
//   Response saliente
//
// Si el Endpoint lanza una excepción no manejada, sube por la cadena
// hasta que alguien la atrape. GlobalExceptionMiddleware la atrapa
// y devuelve una respuesta JSON limpia en lugar de una página de error HTML.
//
// ¿CÓMO FUNCIONA INTERNAMENTE?
// IMiddleware tiene un método InvokeAsync con dos parámetros:
//   - HttpContext context: todo sobre la request y la response actuales
//   - RequestDelegate next: referencia al siguiente middleware en la cadena
// Llamas await next(context) para pasar el control al siguiente.
// Si eso lanza una excepción, la capturas en el catch.
//
// ¿POR QUÉ ILogger?
// Todos los errores deben registrarse para debugging y monitoreo.
// ILogger<T> es el sistema de logging nativo de .NET — escribe en consola
// en desarrollo y se puede configurar para escribir en archivos,
// Application Insights, Serilog, etc. sin cambiar el código.
// El genérico <GlobalExceptionMiddleware> hace que los logs muestren
// de qué clase vienen: "[GlobalExceptionMiddleware] Error: ..."
// =============================================================================

using System.Net;
using System.Text.Json;

namespace ExcelInsights.Api.Middlewares;

/// <summary>
/// Middleware que captura cualquier excepción no manejada en la pipeline
/// y devuelve una respuesta JSON consistente con el error.
/// 
/// Se registra PRIMERO en Program.cs para que envuelva toda la pipeline.
/// </summary>
public class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Se ejecuta para cada request. Envuelve la pipeline en try/catch.
    /// </summary>
    /// <param name="context">Información completa de la request y response HTTP.</param>
    /// <param name="next">El siguiente middleware en la cadena.</param>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            // Pasa el control al siguiente middleware (y eventualmente al endpoint).
            // Si todo va bien, la response ya se escribió y este método termina.
            await next(context);
        }
        catch (Exception ex)
        {
            // Loggeamos el error completo (con stack trace) para debugging interno.
            _logger.LogError(ex, "Error no manejado: {Message}", ex.Message);

            // Escribimos una respuesta JSON limpia al cliente.
            // No exponemos el stack trace — eso es información interna.
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Construye y escribe la respuesta de error al cliente.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // El status code cambia según el tipo de excepción.
        // En el futuro puedes agregar más casos:
        //   FileNotFoundException  → 404
        //   UnauthorizedException  → 401
        var statusCode = ex switch
        {
            NotImplementedException => HttpStatusCode.NotImplemented,     // 501
            ArgumentException       => HttpStatusCode.BadRequest,         // 400
            _                       => HttpStatusCode.InternalServerError  // 500
        };

        // El objeto que el cliente verá serializado como JSON
        var errorResponse = new
        {
            error      = ex.Message,
            statusCode = (int)statusCode,
            type       = ex.GetType().Name
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        // JsonSerializerOptions para que el JSON use camelCase
        // (statusCode, no StatusCode) — convención estándar en APIs REST.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
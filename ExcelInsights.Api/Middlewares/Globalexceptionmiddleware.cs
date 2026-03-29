

using System.Net;
using System.Text.Json;
using ExcelInsights.Domain;

namespace ExcelInsights.Api.Middlewares;

public class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
    {
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {

            await next(context);
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Error no manejado: {Message}", ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            InvalidExcelFileException => HttpStatusCode.BadRequest,
            ExcelTooLargeException    => HttpStatusCode.UnprocessableEntity,
            ArgumentException         => HttpStatusCode.BadRequest,
            NotImplementedException   => HttpStatusCode.NotImplemented,
            _                         => HttpStatusCode.InternalServerError
        };

        var errorResponse = new
        {
            error      = ex.Message,
            statusCode = (int)statusCode,
            type       = ex.GetType().Name
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
// =============================================================================
// Program.cs — Api/
// =============================================================================
//
// ¿QUÉ ES PROGRAM.CS?
// Es el punto de entrada de la aplicación. En .NET 6+ usa "Top-level statements":
// no hay clase Main() explícita — el código se ejecuta directamente.
// El runtime de .NET genera esa clase Main internamente.
//
// TIENE DOS FASES BIEN DIFERENCIADAS:
//
// FASE 1 — CONSTRUCCIÓN (antes de builder.Build()):
//   Se registran todos los servicios en el DI container.
//   La app aún no está corriendo. Solo se configura.
//   Es como preparar los ingredientes antes de cocinar.
//
// FASE 2 — EJECUCIÓN (después de builder.Build()):
//   Se configura la pipeline de middlewares y se arrancan los endpoints.
//   El orden aquí importa: los middlewares se ejecutan en el orden
//   en que se registran con app.Use...()
//
// ¿POR QUÉ Program.cs ES TAN CORTO?
// Porque cada capa tiene su propio DependencyInjection.cs que encapsula
// sus registros. Program.cs solo orquesta — no tiene detalles internos
// de ninguna capa. Si mañana agregas una capa nueva, solo agregas
// una línea aquí.
// =============================================================================

using ExcelInsights.Api.Endpoints;
using ExcelInsights.Api.Middlewares;
using ExcelInsights.Application;
using ExcelInsights.Application.Common;
using ExcelInsights.Infrastructure;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

// =============================================================================
// FASE 1: CONSTRUCCIÓN DEL DI CONTAINER
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// Registra todos los servicios de Application (MediatR + Handlers).
// Definido en Application/DependencyInjection.cs
builder.Services.AddApplication();

// Registra las implementaciones concretas de Infrastructure.
// Definido en Infrastructure/DependencyInjection.cs
builder.Services.AddInfrastructure();

// El middleware necesita estar en el DI para que ASP.NET
// pueda inyectarle el ILogger automáticamente.
builder.Services.AddTransient<GlobalExceptionMiddleware>();

// Habilita la generación de documentación Swagger
// para probar los endpoints en /swagger durante desarrollo.
builder.Services.AddEndpointsApiExplorer();
object value = builder.Services.AddSwaggerGen();

builder.Services.Configure<ExcelInsightsSettings>(
builder.Configuration.GetSection("ExcelInsightsSettings"));
// Registra ExcelInsightsSettings como singleton para inyección directa
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ExcelInsightsSettings>>().Value);
// Configura Kestrel para aceptar archivos grandes según el límite definido en settings.
var maxBytes = builder.Configuration.GetValue<long>("ExcelInsightsSettings:MaxFileSizeBytes");
builder.WebHost.ConfigureKestrel(options =>
options.Limits.MaxRequestBodySize = maxBytes);

// =============================================================================
// FASE 2: PIPELINE Y ARRANQUE
// =============================================================================

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// ORDEN CRÍTICO: el middleware de excepciones va PRIMERO.
// Piénsalo como capas de cebolla: lo que va primero envuelve todo lo demás.
// Si va después de los endpoints, no puede capturar sus errores.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger solo en desarrollo — nunca exponer en producción.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirige HTTP → HTTPS automáticamente.
app.UseHttpsRedirection();

// Registra todos los endpoints definidos en ExcelEndpoints.cs
// bajo el prefijo /api/excel
app.MapExcelEndpoints();

// Arranca el servidor y empieza a escuchar requests.
// Este método bloquea el hilo principal hasta que la app se detiene.
app.Run();
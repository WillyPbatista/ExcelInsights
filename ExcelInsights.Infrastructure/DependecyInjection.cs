/// <summary>
/// DEPENDENCY INJECTION — Infrastructure Layer
///
/// Aquí ocurre el momento más importante de Clean Architecture:
/// conectar las INTERFACES (definidas en Application) con sus
/// IMPLEMENTACIONES CONCRETAS (definidas en Infrastructure).
///
/// El DI container actúa como un directorio:
///   "Cuando alguien pida IExcelParser, dale ClosedXmlExcelParser"
///
/// Por qué esto es poderoso:
///   El Handler solo conoce IExcelParser. No sabe que existe
///   ClosedXmlExcelParser. Si mañana cambias de ClosedXML a EPPlus,
///   solo cambias UNA línea aquí. Nada más en todo el proyecto.
///
/// Tiempos de vida — cuándo se crea una nueva instancia:
///   AddSingleton  → una sola instancia para toda la vida de la app
///   AddScoped     → una instancia por request HTTP  ← usamos este
///   AddTransient  → una instancia nueva cada vez que se pide
///
///   Scoped es el correcto aquí: cada request tiene sus propios
///   servicios, y cuando el request termina, se destruyen.
///
/// EN ISSUE 1 — las clases concretas son stubs (NotImplementedException).
/// El DI levanta sin errores. Si alguien llama un método no implementado,
/// falla ruidosamente — mejor que un fallo silencioso.
/// </summary>

using Microsoft.Extensions.DependencyInjection;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Infrastructure.Excel;
using ExcelInsights.Infrastructure.Pdf;

namespace ExcelInsights.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra todas las implementaciones concretas de Infrastructure.
    /// Cada línea conecta una interfaz de Application con su clase real.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Excel
        services.AddScoped<IExcelParser, ClosedXmlExcelParser>();
        services.AddScoped<IColumnInferrer, ColumnInferrerService>();
        services.AddScoped<IValidationEngine, ValidationEngine>();

        // PDF
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();

        return services;
    }
}
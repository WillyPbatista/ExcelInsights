/// <summary>
/// DEPENDENCY INJECTION — Application Layer
///
/// Este archivo define un método de extensión sobre IServiceCollection.
/// Un método de extensión es una forma de agregar métodos a una clase
/// existente sin modificarla. Aquí extendemos IServiceCollection
/// (el contenedor de dependencias de .NET) con nuestro propio método.
///
/// Por qué cada capa tiene su propio DependencyInjection.cs:
///   - Cada capa es responsable de registrar SUS propias dependencias
///   - La capa Api no necesita saber los detalles internos de Application
///   - Si mañana cambias cómo se registra MediatR, solo tocas este archivo
///   - Program.cs queda limpio — solo llama builder.Services.AddApplication()
///
/// Qué registra Application:
///   - MediatR: escanea el assembly buscando todos los IRequestHandler
///     que existan en este proyecto y los registra automáticamente.
///     No necesitas registrar AnalyzeExcelHandler ni GenerateReportHandler
///     manualmente — MediatR los encuentra solo.
/// </summary>

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace ExcelInsights.Application;

/// <summary>
/// Extensiones del contenedor de dependencias para la capa Application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios de la capa Application.
    ///
    /// Retorna IServiceCollection para permitir el encadenamiento fluido:
    ///   builder.Services
    ///       .AddApplication()
    ///       .AddInfrastructure();
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR escanea el assembly de este proyecto (Application)
        // buscando todas las clases que implementen IRequestHandler<,>.
        // Las registra automáticamente — no hay mapeo manual de handlers.
        //
        // Assembly.GetExecutingAssembly() = el assembly donde se ejecuta
        // este código → ExcelInsights.Application.dll
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
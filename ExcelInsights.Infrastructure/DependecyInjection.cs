// Infrastructure/DependencyInjection.cs

using Microsoft.Extensions.DependencyInjection;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Infrastructure.Excel;
using ExcelInsights.Infrastructure.Excel.Rules;
using ExcelInsights.Infrastructure.Pdf;

namespace ExcelInsights.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ── Excel ────────────────────────────────────────────────────────────
        services.AddScoped<IExcelParser, ClosedXmlExcelParser>();
        services.AddScoped<IColumnInferrer, ColumnInferrerService>();

        // Reglas de validación
        services.AddScoped<IValidationRule, EmptyCellRule>();
        services.AddScoped<IValidationRule, NegativeNumberRule>();
        services.AddScoped<IValidationRule, InvalidEmailRule>();
        services.AddScoped<IValidationRule, InvalidDateRule>();

        // ValidationEngine va después de sus dependencias — más legible
        services.AddScoped<IValidationEngine, ValidationEngine>();

        // ── PDF ──────────────────────────────────────────────────────────────
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();

        return services;
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using ExcelInsights.Application.Common;

namespace ExcelInsights.Application;


public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<ExcelAnalysisOrchestrator>();
        return services;
    }
}
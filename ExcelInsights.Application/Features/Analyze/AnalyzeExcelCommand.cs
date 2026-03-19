/// <summary>
/// COMMAND — AnalyzeExcelCommand
///
/// Un Command en MediatR es simplemente un objeto mensajero.
/// No tiene lógica, solo transporta los datos necesarios para
/// que el Handler pueda hacer su trabajo.
///
/// Implementa IRequest<T> donde T es el tipo de respuesta esperada.
/// MediatR usa esa interfaz para saber dos cosas:
///   1. Que este objeto es un "mensaje" que alguien puede enviar
///   2. Qué tipo de respuesta debe devolver quien lo procese
///
/// Flujo completo:
///   Endpoint → crea este Command → lo pasa a sender.Send()
///   → MediatR lo enruta → AnalyzeExcelHandler lo recibe y responde
/// </summary>

using MediatR;
using ExcelInsights.Application.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace ExcelInsights.Application.Features.Analyze;

/// <summary>
/// Representa la solicitud de analizar un archivo Excel.
/// Se construye en el endpoint y se despacha a través de MediatR.
/// </summary>
public sealed class AnalyzeExcelCommand : IRequest<AnalysisResult>
{
    /// <summary>
    /// Stream del archivo Excel recibido en el request HTTP.
    /// Usamos Stream en lugar de byte[] para no cargar todo el archivo
    /// en memoria de una vez. Más eficiente para archivos grandes.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// Nombre original del archivo (ej: "employees.xlsx").
    /// Se incluye en el reporte para identificar qué archivo fue analizado.
    /// </summary>
    public required string FileName { get; init; }

    [SetsRequiredMembers]
    public AnalyzeExcelCommand(Stream FileStream, string FileName)
    {
        this.FileStream = FileStream;
        this.FileName = FileName;
    }
}
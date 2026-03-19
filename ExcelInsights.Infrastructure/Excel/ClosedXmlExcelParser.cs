using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.Entities;

namespace ExcelInsights.Infrastructure.Excel;

/// <summary>
/// Implementación del parser de Excel usando la librería ClosedXML.
/// STUB — Issue 1. La lógica real se implementa en Issue 2.
/// </summary>
public class ClosedXmlExcelParser : IExcelParser
{
    public Task<ExcelFile> ParseAsync(Stream fileStream, string fileName)
    {
        // Issue 2: aquí irá la lógica con ClosedXML para leer
        // las filas y columnas del archivo Excel.
        throw new NotImplementedException("Implementar en Issue 2 con ClosedXML.");
    }
}

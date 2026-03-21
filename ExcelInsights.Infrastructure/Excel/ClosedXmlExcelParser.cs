using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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
        var excelFile = new XLWorkbook(fileStream);

        var worksheet = excelFile.Worksheets.FirstOrDefault();

        var usedRows = worksheet.RowsUsed();

        var columns = worksheet.Row(1).CellsUsed();
        foreach (var column in columns)
        {
            Console.WriteLine($"Columna: {column.Value}");
        }
        
        Console.WriteLine($"Archivo '{fileName}' parseado con éxito. Total de filas usadas: {usedRows.Count()}. y el archivo tiene {columns.Count()} columnas.");
        var parsedExcel = new ExcelFile
        {
            FileName = fileName,
            TotalRows = usedRows.Count(),
            //Columns = columns.ToList<Column>()

        };
        return Task.FromResult(parsedExcel);
        
    }
}

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
        var rows =  worksheet.RowsUsed().Skip(1); // Omitimos la primera fila que se asume es el encabezado

        foreach (var row in rows)
        {
            Console.WriteLine($"Fila: {row.RowNumber()}");
            foreach (var cell in row.CellsUsed())
            {
                Console.WriteLine($"  Celda: {cell.Address} - Valor: {cell.Value}");
            }
        }

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



using ClosedXML.Excel;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain;

namespace ExcelInsights.Infrastructure.Excel;

public class ClosedXmlExcelParser : IExcelParser
{

    public Task<ExcelFile> ParseAsync(Stream fileStream, string fileName)
    {
        try{
        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheets.First();

        var usedRows = worksheet.RowsUsed().ToList();

        if (usedRows.Count == 0)
        {
            return Task.FromResult(new ExcelFile
            {
                FileName  = fileName,
                TotalRows = 0,
                Columns   = new List<ColumnDefinition>(),
                Rows      = new List<RowData>()
            });
        }

        var headerRow  = usedRows[0];
        var dataRows   = usedRows.Skip(1).ToList();

        var headers = ExtractHeaders(headerRow);
        var rows    = BuildRows(dataRows, headers);
        var columns = BuildColumns(headers, rows);

        var excelFile = new ExcelFile
        {
            FileName  = fileName,
            TotalRows = rows.Count,
            Columns   = columns,
            Rows      = rows
        };

        return Task.FromResult(excelFile);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidExcelFileException("El archivo no es un Excel válido o está dañado: " + ex.Message, ex);
        }
    }


    private static List<string> ExtractHeaders(IXLRow headerRow)
    {
        var headers = new List<string>();

        var lastCellAddress = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCellAddress; col++)
        {

            var cellValue = headerRow.Cell(col).GetString().Trim();

            var headerName = string.IsNullOrWhiteSpace(cellValue)
                ? $"Column_{col}"
                : cellValue;

            headers.Add(headerName);
        }

        return headers;
    }
    private static List<RowData> BuildRows(List<IXLRow> dataRows, List<string> headers)
    {
        var rows = new List<RowData>();

        foreach (var row in dataRows)
        {

            var cells = new Dictionary<string, string>();

            for (int col = 0; col < headers.Count; col++)
            {

                var columnNumber = col + 1;
                var headerName   = headers[col];

                var cellValue = row.Cell(columnNumber).GetString().Trim();

                cells[headerName] = cellValue;
            }

            if (cells.Values.All(v => string.IsNullOrWhiteSpace(v)))
                continue;

            rows.Add(new RowData
            {

                RowIndex = row.RowNumber(),
                Cells    = cells
            });
        }

        return rows;
    }


    private static List<ColumnDefinition> BuildColumns(
        List<string> headers,
        List<RowData> rows)
    {
        var columns = new List<ColumnDefinition>();

        foreach (var header in headers)
        {

            var values = rows
                .Select(r => r.Cells.TryGetValue(header, out var v) ? v : "")
                .ToList();

            columns.Add(new ColumnDefinition
            {
                Name   = header,
                Values = values
            });
        }

        return columns;
    }
}
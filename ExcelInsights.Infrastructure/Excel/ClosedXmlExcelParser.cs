// =============================================================================
// ClosedXmlExcelParser.cs — Infrastructure/Excel/
// =============================================================================
//
// ¿QUÉ HACE ESTE ARCHIVO?
// Implementa el contrato IExcelParser usando la librería ClosedXML.
// Recibe un Stream del archivo Excel, lo lee completo y devuelve
// un ExcelFile con todas las filas y columnas en memoria como strings.
//
// ¿POR QUÉ TODO COMO STRING?
// El parser tiene una sola responsabilidad: extraer datos crudos.
// Decidir si "-3" es un número inválido o si "test@test" es un email
// roto NO es trabajo del parser — es trabajo del IColumnInferrer (Issue 3)
// y del IValidationEngine (Issue 4). Si el parser ya interpreta tipos,
// está haciendo el trabajo de otras dos clases.
//
// FLUJO INTERNO:
//   ParseAsync
//     ├── ExtractHeaders()   → lee fila 1, devuelve nombres de columna
//     ├── BuildRows()        → lee filas 2+, devuelve RowData por fila
//     └── BuildColumns()     → pivota las filas para agrupar valores por columna
//
// CASOS BORDE MANEJADOS:
//   - Excel sin filas de datos (solo header o completamente vacío)
//   - Fila con menos celdas que headers (celda faltante = string vacío)
//   - Header vacío (se reemplaza por "Column_N")
//   - Celdas con fórmulas (ClosedXML las evalúa y GetString() da el resultado)
//   - Celdas vacías (GetString() devuelve "" nunca null)
// =============================================================================

using ClosedXML.Excel;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.Entities;

namespace ExcelInsights.Infrastructure.Excel;

public class ClosedXmlExcelParser : IExcelParser
{
    // -------------------------------------------------------------------------
    // MÉTODO PÚBLICO — punto de entrada del contrato IExcelParser
    // -------------------------------------------------------------------------

    /// <summary>
    /// Lee el stream del archivo Excel y devuelve un ExcelFile con todas
    /// las filas y columnas extraídas como strings crudos.
    /// </summary>
    /// <param name="fileStream">Stream del archivo .xlsx subido por el cliente.</param>
    /// <param name="fileName">Nombre original del archivo, para incluirlo en el resultado.</param>
    public Task<ExcelFile> ParseAsync(Stream fileStream, string fileName)
    {
        // XLWorkbook es el objeto raíz de ClosedXML.
        // Al pasarle un Stream, lee y parsea el archivo en memoria.
        // El using garantiza que los recursos se liberan al salir del bloque
        // aunque ocurra una excepción — evita memory leaks.
        using var workbook = new XLWorkbook(fileStream);

        // Tomamos siempre la primera hoja del archivo.
        // En el futuro esto podría ser configurable (hoja por nombre, por índice).
        // Por ahora la primera hoja es la convención.
        var worksheet = workbook.Worksheets.First();

        // RowsUsed() es crítico: devuelve SOLO las filas que tienen contenido,
        // no las 1,048,576 filas vacías que tiene todo archivo Excel.
        // Sin esto, iterar todas las filas sería extremadamente lento.
        var usedRows = worksheet.RowsUsed().ToList();

        // Si el archivo está completamente vacío (ni siquiera tiene header),
        // devolvemos un ExcelFile vacío sin explotar.
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

        // La primera fila siempre es el header.
        // Las filas de datos empiezan en la segunda.
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

        // Task.FromResult porque el método es sincrónico internamente
        // (ClosedXML no tiene API async) pero el contrato pide Task<ExcelFile>
        // para ser consistente con parsers que sí podrían ser async en el futuro.
        return Task.FromResult(excelFile);
    }

    // -------------------------------------------------------------------------
    // MÉTODO PRIVADO — extrae los nombres de columna de la primera fila
    // -------------------------------------------------------------------------

    /// <summary>
    /// Lee la fila de headers y devuelve los nombres de columna como lista.
    /// Si una celda de header está vacía, usa "Column_N" como fallback
    /// para que ninguna columna quede sin nombre.
    /// </summary>
    private static List<string> ExtractHeaders(IXLRow headerRow)
    {
        var headers = new List<string>();

        // CellsUsed() en la fila del header devuelve las celdas con contenido.
        // Usamos el número de columna de la última celda para saber cuántas
        // columnas tiene el archivo, incluyendo las que puedan estar vacías.
        var lastCellAddress = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCellAddress; col++)
        {
            // GetCell(columnNumber) devuelve la celda en esa posición.
            // GetString() siempre devuelve string, nunca null.
            var cellValue = headerRow.Cell(col).GetString().Trim();

            // Si el header está vacío, le damos un nombre genérico.
            // Esto evita tener columnas sin nombre que luego causen errores
            // al usarlas como clave de diccionario en los RowData.
            var headerName = string.IsNullOrWhiteSpace(cellValue)
                ? $"Column_{col}"
                : cellValue;

            headers.Add(headerName);
        }

        return headers;
    }

    // -------------------------------------------------------------------------
    // MÉTODO PRIVADO — construye las filas de datos
    // -------------------------------------------------------------------------

    /// <summary>
    /// Convierte cada fila del Excel en un RowData con un diccionario
    /// nombre_columna → valor_celda. Todas las celdas son strings.
    /// </summary>
    private static List<RowData> BuildRows(List<IXLRow> dataRows, List<string> headers)
    {
        var rows = new List<RowData>();

        foreach (var row in dataRows)
        {
            // Construimos el diccionario de celdas para esta fila.
            var cells = new Dictionary<string, string>();

            for (int col = 0; col < headers.Count; col++)
            {
                // En Excel las columnas empiezan en 1, nuestro índice en 0.
                // Por eso col + 1 al acceder a la celda.
                var columnNumber = col + 1;
                var headerName   = headers[col];

                // GetString() en una celda vacía devuelve "" (nunca null).
                // Esto es lo que queremos: string vacío, no null.
                // El NullValueRule de Issue 4 detectará estos strings vacíos.
                var cellValue = row.Cell(columnNumber).GetString().Trim();

                cells[headerName] = cellValue;
            }

            // Ignoramos filas completamente vacías.
            // Una fila donde TODAS las celdas son string vacío no aporta datos.
            // Si la dejamos, el validador generaría errores confusos en filas fantasma.
            if (cells.Values.All(v => string.IsNullOrWhiteSpace(v)))
                continue;

            rows.Add(new RowData
            {
                // RowNumber() de ClosedXML devuelve el número de fila real en el Excel.
                // Fila 1 = header, fila 2 = primer dato. Lo guardamos para que
                // los errores de validación puedan decir "error en fila 3 del Excel".
                RowIndex = row.RowNumber(),
                Cells    = cells
            });
        }

        return rows;
    }

    // -------------------------------------------------------------------------
    // MÉTODO PRIVADO — pivota las filas para construir las columnas
    // -------------------------------------------------------------------------

    /// <summary>
    /// A partir de las filas construidas, agrupa los valores por columna.
    /// El resultado es una ColumnDefinition por cada columna del Excel,
    /// cada una con su lista completa de valores para que IColumnInferrer
    /// pueda analizarlos en Issue 3.
    /// </summary>
    private static List<ColumnDefinition> BuildColumns(
        List<string> headers,
        List<RowData> rows)
    {
        var columns = new List<ColumnDefinition>();

        foreach (var header in headers)
        {
            // Para cada columna, recogemos todos sus valores de todas las filas.
            // Si una fila no tiene ese header (no debería pasar pero por seguridad),
            // usamos string vacío como fallback.
            var values = rows
                .Select(r => r.Cells.TryGetValue(header, out var v) ? v : "")
                .ToList();

            columns.Add(new ColumnDefinition
            {
                Name   = header,
                Values = values
                // InferredType y Stats se asignan en Issues 3 y 5 respectivamente.
            });
        }

        return columns;
    }
}
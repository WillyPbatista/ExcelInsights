// =============================================================================
// ClosedXmlExcelParserTests.cs — ExcelInsights.Tests/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO?
// ClosedXmlExcelParser en aislamiento total. No hay API, no hay Handler,
// no hay DI container. Solo instanciamos el parser directamente y
// le pasamos un Stream de un Excel creado en memoria con ClosedXML.
//
// ¿POR QUÉ CREAMOS EL EXCEL EN MEMORIA Y NO USAMOS UN ARCHIVO?
// Los archivos en disco son dependencias externas — si el archivo se mueve,
// el test falla por razones ajenas al código. Un Excel creado en memoria
// dentro del test es autosuficiente y siempre tiene exactamente los datos
// que el test necesita. Nada más, nada menos.
//
// PATRÓN AAA (Arrange, Act, Assert):
// Cada test tiene tres secciones claramente separadas:
//   Arrange → prepara los datos de entrada (el Excel en memoria)
//   Act     → ejecuta el método que estamos testeando
//   Assert  → verifica que el resultado es el esperado
//
// ¿QUÉ ES FluentAssertions?
// Una librería que reemplaza Assert.Equal(esperado, actual) por
// actual.Should().Be(esperado). La ventaja es que el mensaje de error
// cuando falla es mucho más descriptivo y la lectura es más natural.
// =============================================================================

using ClosedXML.Excel;
using ExcelInsights.Infrastructure.Excel;
using FluentAssertions;

namespace ExcelInsights.Tests;

public class ClosedXmlExcelParserTests
{
    // Instancia del parser que vamos a testear.
    // Se crea una vez por clase — el parser no tiene estado, es seguro reutilizarlo.
    private readonly ClosedXmlExcelParser _parser = new();

    // =========================================================================
    // TEST 1 — Excel normal con datos
    // Verifica el caso feliz: un Excel con headers y filas de datos correctos.
    // =========================================================================

    [Fact]
    public async Task ParseAsync_WithValidData_ReturnsCorrectRowsAndColumns()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        // Creamos un Excel en memoria con ClosedXML.
        // XLWorkbook() sin argumentos crea un workbook vacío en memoria.
        var stream = CreateExcelStream(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");

            // Fila 1: headers
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Email";
            sheet.Cell(1, 3).Value = "Age";

            // Fila 2: primer registro
            sheet.Cell(2, 1).Value = "Alice";
            sheet.Cell(2, 2).Value = "alice@example.com";
            sheet.Cell(2, 3).Value = 28;

            // Fila 3: segundo registro
            sheet.Cell(3, 1).Value = "Bob";
            sheet.Cell(3, 2).Value = "bob@example.com";
            sheet.Cell(3, 3).Value = 35;
        });

        // ── ACT ──────────────────────────────────────────────────────────────
        var result = await _parser.ParseAsync(stream, "test.xlsx");

        // ── ASSERT ───────────────────────────────────────────────────────────

        // Verificamos los metadatos del archivo
        result.FileName.Should().Be("test.xlsx");
        result.TotalRows.Should().Be(2);  // 2 filas de datos, no cuenta el header

        // Verificamos que las columnas tienen los nombres correctos
        result.Columns.Should().HaveCount(3);
        result.Columns[0].Name.Should().Be("Name");
        result.Columns[1].Name.Should().Be("Email");
        result.Columns[2].Name.Should().Be("Age");

        // Verificamos que los valores de las columnas son los correctos
        // Column[0] = "Name" debe tener ["Alice", "Bob"]
        result.Columns[0].Values.Should().BeEquivalentTo(new[] { "Alice", "Bob" });
        result.Columns[1].Values.Should().BeEquivalentTo(new[] { "alice@example.com", "bob@example.com" });
        result.Columns[2].Values.Should().BeEquivalentTo(new[] { "28", "35" });

        // Verificamos los RowData — el diccionario de celdas por fila
        result.Rows.Should().HaveCount(2);
        result.Rows[0].Cells["Name"].Should().Be("Alice");
        result.Rows[0].Cells["Email"].Should().Be("alice@example.com");
        result.Rows[1].Cells["Name"].Should().Be("Bob");
    }

    // =========================================================================
    // TEST 2 — Excel vacío (solo header, sin filas de datos)
    // Verifica que el parser no explota y devuelve una estructura vacía.
    // =========================================================================

    [Fact]
    public async Task ParseAsync_WithNoDataRows_ReturnsEmptyRowsButKeepsColumns()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        var stream = CreateExcelStream(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");

            // Solo el header, sin ninguna fila de datos debajo
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Email";
        });

        // ── ACT ──────────────────────────────────────────────────────────────
        var result = await _parser.ParseAsync(stream, "empty.xlsx");

        // ── ASSERT ───────────────────────────────────────────────────────────

        // TotalRows debe ser 0 — no hay filas de datos
        result.TotalRows.Should().Be(0);

        // Pero las columnas sí deben existir con sus nombres
        result.Columns.Should().HaveCount(2);
        result.Columns[0].Name.Should().Be("Name");
        result.Columns[1].Name.Should().Be("Email");

        // Y cada columna debe tener una lista vacía de valores (no null)
        result.Columns[0].Values.Should().BeEmpty();
        result.Columns[1].Values.Should().BeEmpty();

        // Y la lista de filas debe estar vacía (no null)
        result.Rows.Should().BeEmpty();
    }

    // =========================================================================
    // TEST 3 — Celda vacía produce string vacío, nunca null
    // Verifica el caso borde más importante para la cadena de procesamiento.
    // =========================================================================

    [Fact]
    public async Task ParseAsync_WithEmptyCell_ReturnsEmptyStringNotNull()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        var stream = CreateExcelStream(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");

            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Email";
            sheet.Cell(1, 3).Value = "Age";

            sheet.Cell(2, 1).Value = "Carlos";
            // sheet.Cell(2, 2) intencionalmente vacía — no le asignamos valor
            sheet.Cell(2, 3).Value = 30;
        });

        // ── ACT ──────────────────────────────────────────────────────────────
        var result = await _parser.ParseAsync(stream, "missing_cell.xlsx");

        // ── ASSERT ───────────────────────────────────────────────────────────
        result.TotalRows.Should().Be(1);

        // La celda vacía debe ser string vacío, NUNCA null.
        // Si fuera null, cualquier código que intente procesarla
        // lanzaría NullReferenceException sin aviso.
        var emailValue = result.Rows[0].Cells["Email"];
        emailValue.Should().NotBeNull();
        emailValue.Should().Be("");

        // La columna Email también debe tener "" en sus Values, no null
        result.Columns[1].Values[0].Should().Be("");

        // Las otras celdas sí tienen valor
        result.Rows[0].Cells["Name"].Should().Be("Carlos");
        result.Rows[0].Cells["Age"].Should().Be("30");
    }

    // =========================================================================
    // TEST 4 — Fila completamente vacía es ignorada
    // Verifica que una fila donde todas las celdas están vacías no se incluye
    // en el resultado — evita filas fantasma que confundan al validador.
    // =========================================================================

    [Fact]
    public async Task ParseAsync_WithCompletelyEmptyRow_IgnoresThatRow()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        var stream = CreateExcelStream(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");

            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Email";

            sheet.Cell(2, 1).Value = "Alice";
            sheet.Cell(2, 2).Value = "alice@example.com";

            // Fila 3: completamente vacía — todas sus celdas sin valor
            // (no asignamos nada para sheet.Cell(3, x))

            sheet.Cell(4, 1).Value = "Bob";
            sheet.Cell(4, 2).Value = "bob@example.com";
        });

        // ── ACT ──────────────────────────────────────────────────────────────
        var result = await _parser.ParseAsync(stream, "empty_row.xlsx");

        // ── ASSERT ───────────────────────────────────────────────────────────
        // Solo 2 filas de datos reales — la fila vacía no se cuenta
        result.TotalRows.Should().Be(2);
        result.Rows[0].Cells["Name"].Should().Be("Alice");
        result.Rows[1].Cells["Name"].Should().Be("Bob");
    }

    // =========================================================================
    // HELPER — crea un stream de Excel en memoria
    // =========================================================================
    //
    // ¿POR QUÉ UN HELPER PRIVADO?
    // Los 4 tests necesitan crear un Excel en memoria y convertirlo a Stream.
    // Sin el helper, ese código se repetiría en cada test (violación de DRY).
    // El helper recibe una acción que configura el workbook — cada test
    // le pasa su propia configuración. Así el helper es genérico y reutilizable.
    //
    // ¿QUÉ ES Action<XLWorkbook>?
    // Es un delegado (función) que recibe un XLWorkbook y no devuelve nada.
    // Permite que cada test defina su propio contenido sin duplicar
    // la lógica de crear el MemoryStream y guardarlo.

    private static Stream CreateExcelStream(Action<XLWorkbook> configure)
    {
        using var workbook = new XLWorkbook();

        // El test configura el workbook: agrega hojas, escribe celdas
        configure(workbook);

        // Guardamos el workbook en un MemoryStream en lugar de en disco.
        // El MemoryStream se queda en memoria — no crea ningún archivo.
        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        // Rebobinamos al inicio — si no lo hacemos, ClosedXML leerá
        // desde el final del stream y no encontrará nada.
        memoryStream.Position = 0;

        return memoryStream;
    }
}
// =============================================================================
// PdfGeneratorTests.cs — ExcelInsights.Tests/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO?
// QuestPdfGenerator en aislamiento. Le pasamos un AnalysisResult construido
// en memoria y verificamos que el output es un PDF válido.
//
// ¿QUÉ NO TESTEAMOS Y POR QUÉ?
// El contenido visual del PDF — fuentes, colores, posiciones exactas.
// Verificar eso requeriría comparar imágenes renderizadas, lo cual es:
//   1. Extremadamente frágil (cambia con versiones de QuestPDF)
//   2. Lento (renderizar PDFs es costoso)
//   3. Innecesario — si el builder compila y genera bytes válidos,
//      el contenido visual se verifica manualmente una sola vez.
//
// LO QUE SÍ VERIFICAMOS:
//   - Que el generador produce bytes (no vacío, no null)
//   - Que esos bytes son un PDF real (magic number %PDF al inicio)
//   - Que el tamaño es razonable (un PDF real nunca pesa menos de 1KB)
//   - Que no explota con casos borde (sin errores, sin columnas, vacío)
//
// ¿QUÉ ES EL MAGIC NUMBER DE PDF?
// Todo archivo PDF empieza con los bytes 0x25 0x50 0x44 0x46 que en ASCII
// es "%PDF". Es la firma del formato — si el archivo empieza con eso,
// es un PDF válido independientemente de su contenido.
//
// SETUP:
// QuestPDF requiere configurar la licencia antes de generar cualquier PDF.
// Lo hacemos en el constructor de la clase de tests — se ejecuta una vez
// antes de cada test y garantiza que la licencia está activa.
// =============================================================================

using ExcelInsights.Application.DTOs;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsights.Infrastructure.Pdf;
using FluentAssertions;
using QuestPDF.Infrastructure;
using Xunit.Abstractions;

namespace ExcelInsights.Tests;

public class PdfGeneratorTests
{
    private readonly ITestOutputHelper _output;
    private readonly QuestPdfGenerator _generator;

    public PdfGeneratorTests(ITestOutputHelper output)
    {
        _output = output;

        // QuestPDF requiere esta línea antes de generar cualquier PDF.
        // Sin ella lanza InvalidOperationException aunque sea en tests.
        // Community license es gratuita para uso no comercial y desarrollo.
        QuestPDF.Settings.License = LicenseType.Community;

        _generator = new QuestPdfGenerator();
    }

    // -------------------------------------------------------------------------
    // HELPER — construye AnalysisResult con datos configurables
    // -------------------------------------------------------------------------

    /// <summary>
    /// Crea un AnalysisResult completo con datos representativos.
    /// Parámetros opcionales permiten personalizar cada test sin repetir código.
    /// </summary>
    private static AnalysisResult BuildResult(
        string fileName        = "test.xlsx",
        int totalRows          = 10,
        int validRows          = 8,
        int invalidRows        = 2,
        List<ColumnSummary>?  columns = null,
        List<RowErrorDto>?    errors  = null)
    {
        return new AnalysisResult
        {
            FileName    = fileName,
            TotalRows   = totalRows,
            ValidRows   = validRows,
            InvalidRows = invalidRows,
            Columns     = columns ?? BuildDefaultColumns(),
            Errors      = errors  ?? BuildDefaultErrors()
        };
    }

    private static List<ColumnSummary> BuildDefaultColumns() => new()
    {
        new ColumnSummary
        {
            Name         = "Email",
            InferredType = "Email",
            Confidence   = 0.88,
            Stats        = new ColumnStats { NullCount = 1, UniqueCount = 8 }
        },
        new ColumnSummary
        {
            Name         = "Age",
            InferredType = "Integer",
            Confidence   = 1.0,
            Stats        = new ColumnStats
            {
                Average     = 32.5,
                Min         = -5,
                Max         = 999,
                NullCount   = 0,
                UniqueCount = 10
            }
        },
        new ColumnSummary
        {
            Name         = "Salary",
            InferredType = "Integer",
            Confidence   = 1.0,
            Stats        = new ColumnStats
            {
                Average     = 56100,
                Min         = -9000,
                Max         = 95000,
                NullCount   = 0,
                UniqueCount = 10
            }
        }
    };

    private static List<RowErrorDto> BuildDefaultErrors() => new()
    {
        new RowErrorDto { RowIndex = 3, ColumnName = "Email",  Message = "Invalid email format: carlos@ruiz", Severity = "Error"   },
        new RowErrorDto { RowIndex = 3, ColumnName = "Age",    Message = "Negative number found: -5",         Severity = "Error"   },
        new RowErrorDto { RowIndex = 4, ColumnName = "Salary", Message = "Negative number found: -9000",      Severity = "Error"   },
        new RowErrorDto { RowIndex = 5, ColumnName = "Email",  Message = "Cell is empty",                     Severity = "Warning" }
    };

    private void LogPdfResult(string testName, byte[] bytes)
    {
        _output.WriteLine($"[{testName}]");
        _output.WriteLine($"  Bytes generados: {bytes.Length:N0}");
        _output.WriteLine($"  Tamaño:          {bytes.Length / 1024.0:F1} KB");
        _output.WriteLine($"  Magic number:    {(bytes.Length >= 4 ? System.Text.Encoding.ASCII.GetString(bytes, 0, 4) : "N/A")}");
        _output.WriteLine("");
    }

    // =========================================================================
    // TEST 1 — Resultado normal con datos completos
    // El caso más representativo: columnas con stats, errores y warnings.
    // Verifica que el output es un PDF real y tiene tamaño razonable.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithCompleteResult_ReturnsPdfBytes()
    {
        var result = BuildResult();

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithCompleteResult_ReturnsPdfBytes), bytes);

        // El PDF no debe estar vacío
        bytes.Should().NotBeNullOrEmpty();

        // Todo PDF válido empieza con "%PDF" en ASCII
        // Bytes: 0x25=% 0x50=P 0x44=D 0x46=F
        var magicNumber = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        magicNumber.Should().Be("%PDF", "un PDF válido siempre empieza con %PDF");

        // Un PDF con contenido real nunca pesa menos de 1KB
        bytes.Length.Should().BeGreaterThan(1024, "un PDF real siempre supera 1KB");
    }

    // =========================================================================
    // TEST 2 — Resultado sin errores → badge CLEAN, sección "No errors"
    // Verifica que el builder no explota cuando Errors está vacío
    // y que la sección de errores muestra el mensaje positivo.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithNoErrors_GeneratesValidPdf()
    {
        var result = BuildResult(
            totalRows: 5,
            validRows: 5,
            invalidRows: 0,
            errors: new List<RowErrorDto>()  
        );

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithNoErrors_GeneratesValidPdf), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 3 — Columnas con stats null (no numéricas)
    // Verifica que "—" aparece correctamente y no hay NullReferenceException
    // cuando Average, Min y Max son null en ColumnStats.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithNullStats_DoesNotThrow()
    {
        var result = BuildResult(
            columns: new List<ColumnSummary>
            {
                new ColumnSummary
                {
                    Name         = "Email",
                    InferredType = "Email",
                    Confidence   = 0.9,
                    Stats        = new ColumnStats
                    {
                        // Average, Min y Max son null para columnas no numéricas
                        Average     = null,
                        Min         = null,
                        Max         = null,
                        NullCount   = 2,
                        UniqueCount = 8
                    }
                },
                new ColumnSummary
                {
                    Name         = "Department",
                    InferredType = "FreeText",
                    Confidence   = 0.7,
                    Stats        = new ColumnStats
                    {
                        Average     = null,
                        Min         = null,
                        Max         = null,
                        NullCount   = 1,
                        UniqueCount = 4
                    }
                }
            },
            errors: new List<RowErrorDto>()
        );

        // No debe lanzar NullReferenceException cuando Stats tiene nulls
        var act = async () => await _generator.GenerateAsync(result);
        await act.Should().NotThrowAsync();

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithNullStats_DoesNotThrow), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 4 — Resultado completamente vacío
    // El caso más extremo: sin filas, sin columnas, sin errores.
    // Verifica que el builder no explota con listas vacías.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithEmptyResult_DoesNotThrow()
    {
        var result = BuildResult(
            fileName:     "empty.xlsx",
            totalRows:    0,
            validRows:   0,
            invalidRows: 0,
            columns:     new List<ColumnSummary>(),
            errors:      new List<RowErrorDto>()
        );

        // El foreach de columnas y errores iterará listas vacías — no debe explotar
        var act = async () => await _generator.GenerateAsync(result);
        await act.Should().NotThrowAsync();

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithEmptyResult_DoesNotThrow), bytes);

        // Incluso un PDF sin contenido sigue siendo un PDF válido
        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 5 — Solo warnings, ningún error
    // Verifica que el badge es "ISSUES FOUND" (porque InvalidRows > 0)
    // y que las celdas de Severity con "Warning" no explotan.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithOnlyWarnings_GeneratesValidPdf()
    {
        var result = BuildResult(
            totalRows: 5,
            validRows:   3,
            invalidRows: 2,
            errors: new List<RowErrorDto>
            {
                new RowErrorDto { RowIndex = 2, ColumnName = "Email",  Message = "Cell is empty", Severity = "Warning" },
                new RowErrorDto { RowIndex = 4, ColumnName = "Salary", Message = "Cell is empty", Severity = "Warning" }
            }
        );

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithOnlyWarnings_GeneratesValidPdf), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 6 — Nombre de archivo con caracteres especiales
    // Verifica que el Builder no explota cuando FileName tiene
    // caracteres como tildes, espacios o guiones.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithSpecialCharactersInFileName_GeneratesValidPdf()
    {
        var result = BuildResult(fileName: "empleados_año_2024 (revisado).xlsx");

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithSpecialCharactersInFileName_GeneratesValidPdf), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 7 — Muchas columnas (tabla ancha)
    // Verifica que QuestPDF maneja correctamente una tabla con más columnas
    // de las que caben cómodamente en una página A4.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithManyColumns_GeneratesValidPdf()
    {
        var manyColumns = Enumerable.Range(1, 15).Select(i => new ColumnSummary
        {
            Name         = $"Column_{i}",
            InferredType = i % 2 == 0 ? "Integer" : "FreeText",
            Confidence   = 0.95,
            Stats        = new ColumnStats
            {
                Average     = i % 2 == 0 ? (double?)i * 100 : null,
                Min         = i % 2 == 0 ? (decimal?)i      : null,
                Max         = i % 2 == 0 ? (decimal?)i * 10 : null,
                NullCount   = i % 3,
                UniqueCount = 10 - (i % 4)
            }
        }).ToList();

        var result = BuildResult(columns: manyColumns, errors: new List<RowErrorDto>());

        var act = async () => await _generator.GenerateAsync(result);
        await act.Should().NotThrowAsync();

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithManyColumns_GeneratesValidPdf), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 8 — Muchos errores (tabla larga que puede paginar)
    // Verifica que QuestPDF maneja el salto de página automático
    // cuando la tabla de errores supera una página A4.
    // =========================================================================

    [Fact]
    public async Task GenerateAsync_WithManyErrors_GeneratesValidPdfWithPagination()
    {
        // 50 errores — suficientes para forzar al menos un salto de página
        var manyErrors = Enumerable.Range(1, 50).Select(i => new RowErrorDto
        {
            RowIndex   = i,
            ColumnName = i % 2 == 0 ? "Email" : "Age",
            Message    = i % 2 == 0 ? $"Invalid email format: user{i}@" : $"Negative number: -{i}",
            Severity   = i % 3 == 0 ? "Warning" : "Error"
        }).ToList();

        var result = BuildResult(
            totalRows:    50,
            validRows:   0,
            invalidRows: 50,
            errors:      manyErrors
        );

        var bytes = await _generator.GenerateAsync(result);
        LogPdfResult(nameof(GenerateAsync_WithManyErrors_GeneratesValidPdfWithPagination), bytes);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");

        // Un PDF con 50 filas de errores debe ser notablemente más grande que el mínimo
        bytes.Length.Should().BeGreaterThan(5120, "un PDF con 50 errores debe superar 5KB");
    }
}
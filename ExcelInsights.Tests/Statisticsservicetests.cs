// =============================================================================
// StatisticsServiceTests.cs — ExcelInsights.Tests/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO?
// StatisticsService.Calculate() en aislamiento total.
// El método es estático y puro — recibe una ColumnDefinition,
// devuelve un ColumnStats. No hay DI, no hay API, no hay base de datos.
//
// ¿POR QUÉ SON IMPORTANTES ESTOS TESTS?
// Los cálculos numéricos son los más fáciles de romper silenciosamente:
//   - Un Average que incluye celdas vacías da un resultado incorrecto
//     pero la API sigue respondiendo 200 sin ningún error visible.
//   - Un NullCount que está off-by-one nunca lanza excepción.
//   - Un Min que explota con lista vacía solo falla en producción.
// Sin tests, estos bugs llegan al PDF del cliente.
//
// HELPER BuildColumn:
// Crea una ColumnDefinition con el tipo inferido y los valores dados.
// Centraliza la construcción para que los tests sean legibles.
// =============================================================================

using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain.Services;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsights.Enums.Entities;
using FluentAssertions;
using Xunit.Abstractions;

namespace ExcelInsights.Tests;

public class StatisticsServiceTests
{
    private readonly ITestOutputHelper _output;

    public StatisticsServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // -------------------------------------------------------------------------
    // HELPER
    // -------------------------------------------------------------------------

    private static ColumnDefinition BuildColumn(string name, DataType type, List<string> values)
    {
        var column = new ColumnDefinition
        {
            Name   = name,
            Values = values
        };
        column.SetInferredType(new InferredType(type, 1.0));
        return column;
    }

    private void LogStats(string testName, ColumnStats stats)
    {
        _output.WriteLine($"[{testName}]");
        _output.WriteLine($"  Average:     {(stats.Average.HasValue    ? stats.Average.ToString()    : "null")}");
        _output.WriteLine($"  Min:         {(stats.Min.HasValue        ? stats.Min.ToString()        : "null")}");
        _output.WriteLine($"  Max:         {(stats.Max.HasValue        ? stats.Max.ToString()        : "null")}");
        _output.WriteLine($"  NullCount:   {stats.NullCount}");
        _output.WriteLine($"  UniqueCount: {stats.UniqueCount}");
        _output.WriteLine("");
    }

    // =========================================================================
    // TEST 1 — Columna Integer normal
    // Caso base: valores limpios, sin vacíos, sin negativos.
    // Verifica que Average, Min y Max se calculan correctamente.
    // =========================================================================

    [Fact]
    public void Calculate_IntegerColumn_ReturnsCorrectStats()
    {
        var column = BuildColumn("Age", DataType.Integer,
            new List<string> { "28", "35", "22", "45", "30" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_IntegerColumn_ReturnsCorrectStats), stats);

        // (28 + 35 + 22 + 45 + 30) / 5 = 32
        stats.Average.Should().BeApproximately(32.0, precision: 0.01);
        stats.Min.Should().Be(22);
        stats.Max.Should().Be(45);
        stats.NullCount.Should().Be(0);
        stats.UniqueCount.Should().Be(5);
    }

    // =========================================================================
    // TEST 2 — Columna Integer con negativos
    // Los negativos son errores de negocio (Issue 4 los detecta)
    // pero estadísticamente son valores válidos que deben incluirse.
    // Min debe ser el negativo, Average debe incluirlo.
    // =========================================================================

    [Fact]
    public void Calculate_IntegerWithNegatives_IncludesNegativesInStats()
    {
        // Datos reales de la columna Age de employees.xlsx
        var column = BuildColumn("Age", DataType.Integer,
            new List<string> { "28", "35", "-5", "30", "25", "22", "999", "31", "27", "33" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_IntegerWithNegatives_IncludesNegativesInStats), stats);

        // Min debe ser -5, no 22 — el negativo es un valor real de los datos
        stats.Min.Should().Be(-5);
        stats.Max.Should().Be(999);

        // Average incluye todos los valores incluyendo negativos y outliers
        // (28+35-5+30+25+22+999+31+27+33) / 10 = 122.5
        stats.Average.Should().BeApproximately(122.5, precision: 0.01);
        stats.NullCount.Should().Be(0);
    }

    // =========================================================================
    // TEST 3 — Columna Integer con celdas vacías
    // Las celdas vacías NO deben incluirse en el cálculo de Average.
    // Este es el bug más común: si incluyes los vacíos como 0,
    // el Average queda artificialmente bajo.
    // =========================================================================

    [Fact]
    public void Calculate_IntegerWithEmptyCells_ExcludesEmptiesFromAverage()
    {
        var column = BuildColumn("Salary", DataType.Integer,
            new List<string> { "55000", "", "72000", "", "48000" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_IntegerWithEmptyCells_ExcludesEmptiesFromAverage), stats);

        // NullCount debe ser 2 — hay dos celdas vacías
        stats.NullCount.Should().Be(2);

        // Average debe calcularse solo con los 3 valores no vacíos
        // (55000 + 72000 + 48000) / 3 = 58333.33
        // NO (55000 + 0 + 72000 + 0 + 48000) / 5 = 35000
        stats.Average.Should().BeApproximately(58333.33, precision: 0.5);

        stats.Min.Should().Be(48000);
        stats.Max.Should().Be(72000);
        stats.UniqueCount.Should().Be(3); // solo los 3 valores no vacíos
    }

    // =========================================================================
    // TEST 4 — Columna Decimal
    // Verifica que los decimales se preservan sin truncar a int.
    // =========================================================================

    [Fact]
    public void Calculate_DecimalColumn_PreservesDecimalPrecision()
    {
        var column = BuildColumn("Price", DataType.Decimal,
            new List<string> { "29.99", "149.50", "89.99", "425.00" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_DecimalColumn_PreservesDecimalPrecision), stats);

        // Min debe ser 29.99, no 29 — sin truncar a int
        stats.Min.Should().Be(29.99m);
        stats.Max.Should().Be(425.00m);

        // (29.99 + 149.50 + 89.99 + 425.00) / 4 = 173.62
        stats.Average.Should().BeApproximately(173.62, precision: 0.01);
    }

    // =========================================================================
    // TEST 5 — Columna Email (no numérica)
    // Para columnas no numéricas, Average/Min/Max deben ser null.
    // Pero NullCount y UniqueCount sí deben calcularse.
    // =========================================================================

    [Fact]
    public void Calculate_EmailColumn_ReturnsNullNumericStats()
    {
        var column = BuildColumn("Email", DataType.Email,
            new List<string>
            {
                "alice@company.com",
                "bob@corp.io",
                "",                     // vacío
                "carlos@company.com",
                "alice@company.com"     // duplicado
            });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_EmailColumn_ReturnsNullNumericStats), stats);

        // Métricas numéricas no aplican a emails
        stats.Average.Should().BeNull();
        stats.Min.Should().BeNull();
        stats.Max.Should().BeNull();

        // Pero NullCount y UniqueCount sí deben calcularse
        stats.NullCount.Should().Be(1);    // un vacío
        stats.UniqueCount.Should().Be(3);  // alice, bob, carlos (sin duplicado ni vacío)
    }

    // =========================================================================
    // TEST 6 — Columna completamente vacía
    // Todos los valores son string vacío.
    // NullCount debe ser igual al total de valores.
    // Los cálculos numéricos no deben explotar — deben devolver null.
    // =========================================================================

    [Fact]
    public void Calculate_AllEmptyCells_ReturnsNullStatsAndCorrectNullCount()
    {
        var column = BuildColumn("Department", DataType.FreeText,
            new List<string> { "", "", "", "" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_AllEmptyCells_ReturnsNullStatsAndCorrectNullCount), stats);

        stats.NullCount.Should().Be(4);
        stats.UniqueCount.Should().Be(0);
        stats.Average.Should().BeNull();
        stats.Min.Should().BeNull();
        stats.Max.Should().BeNull();
    }

    // =========================================================================
    // TEST 7 — Columna Integer completamente vacía
    // Caso borde crítico: tipo Integer pero sin valores parseables.
    // Sin la guarda "parsed.Count > 0", parsed.Min() explota aquí.
    // =========================================================================

    [Fact]
    public void Calculate_IntegerColumnAllEmpty_DoesNotThrow()
    {
        var column = BuildColumn("Age", DataType.Integer,
            new List<string> { "", "", "" });

        // El servicio NO debe lanzar excepción — debe devolver nulls
        var act = () => StatisticsService.Calculate(column);
        act.Should().NotThrow();

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_IntegerColumnAllEmpty_DoesNotThrow), stats);

        stats.Min.Should().BeNull();
        stats.Max.Should().BeNull();
        stats.Average.Should().BeNull();
        stats.NullCount.Should().Be(3);
    }

    // =========================================================================
    // TEST 8 — UniqueCount con duplicados
    // Verifica que UniqueCount cuenta valores distintos, no totales.
    // =========================================================================

    [Fact]
    public void Calculate_ColumnWithDuplicates_ReturnsCorrectUniqueCount()
    {
        var column = BuildColumn("Department", DataType.FreeText,
            new List<string>
            {
                "Engineering",
                "Marketing",
                "Engineering",  // duplicado
                "Sales",
                "Marketing",    // duplicado
                "Engineering"   // duplicado
            });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_ColumnWithDuplicates_ReturnsCorrectUniqueCount), stats);

        // 6 valores totales pero solo 3 distintos
        stats.UniqueCount.Should().Be(3);
        stats.NullCount.Should().Be(0);
    }

    // =========================================================================
    // TEST 9 — Columna con un solo valor
    // Min, Max y Average deben ser todos iguales.
    // =========================================================================

    [Fact]
    public void Calculate_SingleValue_MinMaxAverageAreEqual()
    {
        var column = BuildColumn("Age", DataType.Integer,
            new List<string> { "42" });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_SingleValue_MinMaxAverageAreEqual), stats);

        stats.Min.Should().Be(42);
        stats.Max.Should().Be(42);
        stats.Average.Should().BeApproximately(42.0, precision: 0.01);
        stats.UniqueCount.Should().Be(1);
        stats.NullCount.Should().Be(0);
    }

    // =========================================================================
    // TEST 10 — Datos reales de Salary en employees.xlsx
    // Test de integración que verifica el servicio con datos del mundo real.
    // Estos son los valores exactos que el parser devolvería de employees.xlsx.
    // =========================================================================

    [Fact]
    public void Calculate_RealSalaryColumn_ReturnsExpectedStats()
    {
        // Columna Salary de employees.xlsx — incluye el -9000 negativo
        var column = BuildColumn("Salary", DataType.Integer,
            new List<string>
            {
                "55000", "72000", "48000", "-9000",
                "61000", "43000", "95000", "58000",
                "67000", "71000"
            });

        var stats = StatisticsService.Calculate(column);
        LogStats(nameof(Calculate_RealSalaryColumn_ReturnsExpectedStats), stats);

        stats.Min.Should().Be(-9000);
        stats.Max.Should().Be(95000);

        // (-9000+55000+72000+48000+61000+43000+95000+58000+67000+71000) / 10 = 56100
        stats.Average.Should().BeApproximately(56100.0, precision: 0.5);
        stats.NullCount.Should().Be(0);
        stats.UniqueCount.Should().Be(10); // todos distintos
    }
}
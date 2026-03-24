// =============================================================================
// ColumnInferrerServiceTests.cs — ExcelInsights.Tests/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO?
// ColumnInferrerService en aislamiento total. No hay Handler, no hay API,
// no hay parser. Solo instanciamos el servicio directamente y le pasamos
// listas de strings como si fueran los valores de una columna real.
//
// ¿QUÉ ES ITestOutputHelper?
// Es la interfaz de xUnit para escribir logs dentro de un test.
// A diferencia de Console.WriteLine (que xUnit captura y oculta),
// ITestOutputHelper muestra el output directamente en el panel de tests
// del IDE y en la consola de "dotnet test".
// Se inyecta por el constructor — xUnit lo provee automáticamente.
//
// ¿POR QUÉ LOGGEAR EN LOS TESTS?
// Cuando un test falla, los logs te muestran exactamente qué valores
// recibió el servicio y qué devolvió. Sin logs, solo ves "expected X but got Y"
// sin contexto de por qué pasó eso.
//
// PATRÓN DE CADA TEST:
//   1. Log de entrada  → qué valores le estamos pasando
//   2. Act             → llamar a Infer()
//   3. Log de salida   → qué devolvió el servicio
//   4. Assert          → verificar que el resultado es el esperado
// =============================================================================

using ExcelInsights.Enums.Entities;
using ExcelInsights.Infrastructure.Excel;
using FluentAssertions;
using Xunit.Abstractions;

namespace ExcelInsights.Tests;

public class ColumnInferrerServiceTests
{
    // -------------------------------------------------------------------------
    // SETUP
    // -------------------------------------------------------------------------

    // El servicio que vamos a testear.
    // No tiene estado interno — es seguro reutilizar la misma instancia
    // en todos los tests de esta clase.
    private readonly ColumnInferrerService _inferrer = new();

    // ITestOutputHelper es el sistema de logging de xUnit.
    // Muestra output en el panel de tests del IDE y en "dotnet test".
    private readonly ITestOutputHelper _output;

    // xUnit inyecta ITestOutputHelper automáticamente por el constructor.
    // No necesitas registrarlo en ningún DI container.
    public ColumnInferrerServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // -------------------------------------------------------------------------
    // HELPER PRIVADO — centraliza el logging de entrada y salida
    // -------------------------------------------------------------------------

    /// <summary>
    /// Llama a Infer(), loggea la entrada y la salida, y devuelve el resultado.
    /// Todos los tests lo usan en lugar de llamar a _inferrer.Infer() directo
    /// para garantizar que siempre hay logs visibles cuando un test falla.
    /// </summary>
    private Domain.ValueObjects.InferredType InferAndLog(
        string testName,
        IEnumerable<string> values)
    {
        var valuesList = values.ToList();

        // Log de entrada: qué le estamos pasando al servicio
        _output.WriteLine($"[{testName}]");
        _output.WriteLine($"  Entrada ({valuesList.Count} valores): [{string.Join(", ", valuesList.Select(v => $"\"{v}\""))}]");

        var result = _inferrer.Infer(valuesList);

        // Log de salida: qué devolvió el servicio
        _output.WriteLine($"  Resultado → DataType: {result.DataType}, Confidence: {result.Confidence:P0}");
        _output.WriteLine("");

        return result;
    }

    // =========================================================================
    // TEST 1 — Todos los valores son enteros
    // Caso más básico: columna "Age" con valores numéricos enteros.
    // Esperamos Integer con confianza 1.0 (el 100% de los valores son enteros).
    // =========================================================================

    [Fact]
    public void Infer_AllIntegers_ReturnsIntegerWithFullConfidence()
    {
        var values = new[] { "28", "35", "22", "45", "30" };

        var result = InferAndLog(nameof(Infer_AllIntegers_ReturnsIntegerWithFullConfidence), values);

        result.DataType.Should().Be(DataType.Integer);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 2 — Todos los valores son decimales puros (no enteros)
    // Un decimal puro como "28.5" no es parseable como long,
    // por eso solo vota por Decimal y no por Integer.
    // =========================================================================

    [Fact]
    public void Infer_AllDecimals_ReturnsDecimalWithFullConfidence()
    {
        var values = new[] { "28.5", "35.99", "22.1", "45.75" };

        var result = InferAndLog(nameof(Infer_AllDecimals_ReturnsDecimalWithFullConfidence), values);

        result.DataType.Should().Be(DataType.Decimal);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 3 — Todos los valores son emails válidos
    // Columna típica de "Email" en un Excel de empleados o clientes.
    // =========================================================================

    [Fact]
    public void Infer_AllEmails_ReturnsEmailWithFullConfidence()
    {
        var values = new[]
        {
            "alice@company.com",
            "bob.smith@corp.io",
            "carlos@ruiz.mx",
            "diana@example.org"
        };

        var result = InferAndLog(nameof(Infer_AllEmails_ReturnsEmailWithFullConfidence), values);

        result.DataType.Should().Be(DataType.Email);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 4 — Todos los valores son fechas
    // DateTime.TryParse acepta múltiples formatos, verificamos eso aquí.
    // =========================================================================

    [Fact]
    public void Infer_AllDates_ReturnsDateWithFullConfidence()
    {
        var values = new[]
        {
            "2024-03-15",
            "2023-07-01",
            "2022-11-20",
            "2021-01-10"
        };

        var result = InferAndLog(nameof(Infer_AllDates_ReturnsDateWithFullConfidence), values);

        result.DataType.Should().Be(DataType.Date);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 5 — Todos los valores son booleanos (variantes del mundo real)
    // bool.TryParse solo acepta "True"/"False". Nuestro IsBoolean acepta
    // además: yes, no, si, sí, 1, 0 — que son comunes en Excels reales.
    // =========================================================================

    [Fact]
    public void Infer_AllBooleans_ReturnsBooleanWithFullConfidence()
    {
        // Mezclamos variantes para verificar que todas son reconocidas
        var values = new[] { "true", "false", "yes", "no", "1", "0", "si", "sí" };

        var result = InferAndLog(nameof(Infer_AllBooleans_ReturnsBooleanWithFullConfidence), values);

        result.DataType.Should().Be(DataType.Boolean);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 6 — Mayoría de emails con algunos inválidos
    // Simula employees.xlsx donde la columna Email tiene 2 entradas rotas.
    // 8 emails válidos de 10 → confianza esperada: 0.8
    // =========================================================================

    [Fact]
    public void Infer_MostlyEmails_ReturnsEmailWithPartialConfidence()
    {
        var values = new[]
        {
            "alice@company.com",    // válido
            "bob@corp.io",          // válido
            "carlos@ruiz.mx",       // válido
            "diana@example.org",    // válido
            "evan@test.com",        // válido
            "fiona@mail.net",       // válido
            "george@site.co",       // válido
            "hannah@inbox.com",     // válido
            "notanemail",           // inválido — sin @
            "also-invalid"          // inválido — sin @ ni punto de dominio
        };

        var result = InferAndLog(nameof(Infer_MostlyEmails_ReturnsEmailWithPartialConfidence), values);

        result.DataType.Should().Be(DataType.Email);

        // 8 de 10 son emails válidos → confianza 0.8
        result.Confidence.Should().BeApproximately(0.8, precision: 0.01);
    }

    // =========================================================================
    // TEST 7 — Enteros mezclados con celdas vacías
    // Las celdas vacías NO deben votar ni reducir la confianza.
    // 4 enteros + 3 vacíos → confianza debe ser 1.0, no 4/7 = 0.57
    // =========================================================================

    [Fact]
    public void Infer_IntegersWithEmptyCells_IgnoresEmptiesInConfidence()
    {
        var values = new[] { "28", "", "35", "", "22", "", "45" };

        var result = InferAndLog(nameof(Infer_IntegersWithEmptyCells_IgnoresEmptiesInConfidence), values);

        result.DataType.Should().Be(DataType.Integer);

        // Los 3 vacíos no cuentan → 4 enteros / 4 no-vacíos = 1.0
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 8 — "1", "0", "1", "0" debe ser Boolean, no Integer
    // Este test verifica la jerarquía de especificidad.
    // "1" y "0" son parseables como long (Integer), PERO también son booleanos.
    // Boolean debe ganar porque se evalúa primero en la jerarquía.
    // =========================================================================

    [Fact]
    public void Infer_OnesAndZeros_ReturnsBooleanNotInteger()
    {
        var values = new[] { "1", "0", "1", "0", "1", "1" };

        var result = InferAndLog(nameof(Infer_OnesAndZeros_ReturnsBooleanNotInteger), values);

        // Si este test falla devolviendo Integer, significa que la jerarquía
        // en DetermineDataType no está evaluando Boolean antes que Integer.
        result.DataType.Should().Be(DataType.Boolean);
    }

    // =========================================================================
    // TEST 9 — Mezcla sin patrón claro → FreeText
    // Cuando los valores son muy heterogéneos, ningún tipo supera el umbral
    // del 60% de confianza y la columna cae en FreeText.
    // =========================================================================

    [Fact]
    public void Infer_MixedTypes_ReturnsFreeText()
    {
        var values = new[]
        {
            "hello",            // texto
            "28",               // integer
            "alice@mail.com",   // email
            "2024-01-15",       // date
            "world",            // texto
            "goodbye",          // texto
            "yes",              // boolean
            "foo bar"           // texto
        };

        var result = InferAndLog(nameof(Infer_MixedTypes_ReturnsFreeText), values);

        // Ningún tipo tiene mayoría clara → FreeText
        result.DataType.Should().Be(DataType.FreeText);

        // La confianza en FreeText es la del tipo que más votos tuvo
        // pero no superó el umbral — debe ser menor a 0.6
        result.Confidence.Should().BeLessThan(0.6);
    }

    // =========================================================================
    // TEST 10 — Columna completamente vacía → Unknown
    // Una columna donde todas las celdas están vacías no tiene datos
    // para inferir. Debe devolver Unknown, NO FreeText.
    // Unknown = "no hay datos". FreeText = "hay datos pero sin patrón".
    // =========================================================================

    [Fact]
    public void Infer_AllEmptyCells_ReturnsUnknown()
    {
        var values = new[] { "", "", "", "" };

        var result = InferAndLog(nameof(Infer_AllEmptyCells_ReturnsUnknown), values);

        // Si este test devuelve FreeText en lugar de Unknown, significa que
        // el early return para nonEmpty.Count == 0 no está funcionando.
        result.DataType.Should().Be(DataType.Unknown);
        result.Confidence.Should().Be(0.0);
    }

    // =========================================================================
    // TEST 11 — Enteros negativos son Integer, no FreeText
    // "-3" y "-9000" son enteros negativos válidos.
    // long.TryParse los acepta. Este test verifica ese comportamiento.
    // =========================================================================

    [Fact]
    public void Infer_NegativeIntegers_ReturnsInteger()
    {
        // Datos de employees.xlsx: Age con valores negativos
        var values = new[] { "28", "-5", "30", "35", "22" };

        var result = InferAndLog(nameof(Infer_NegativeIntegers_ReturnsInteger), values);

        result.DataType.Should().Be(DataType.Integer);
        result.Confidence.Should().Be(1.0);
    }

    // =========================================================================
    // TEST 12 — Verificación end-to-end con columnas reales de employees.xlsx
    // Este test simula exactamente los valores que llegarían del parser
    // después de leer employees.xlsx. Es el test de integración más cercano
    // a lo que sucede en producción sin levantar la API completa.
    // =========================================================================

    [Fact]
    public void Infer_RealEmployeeColumns_ReturnsExpectedTypes()
    {
        // ── Columna Email de employees.xlsx ──
        // 2 emails inválidos de 10 → confianza 0.8
        var emailValues = new[]
        {
            "alice@company.com", "bob.smith@corp.com", "carlos@ruiz",
            "diana@company.com", "", "fiona@grant.io",
            "george@hill.com", "notanemail", "ivan@petrov.ru", "julia@santos.br"
        };

        var emailResult = InferAndLog("Email column", emailValues);
        emailResult.DataType.Should().Be(DataType.Email);
        emailResult.Confidence.Should().BeGreaterThan(0.6);

        // ── Columna Age de employees.xlsx ──
        // Todos enteros (incluyendo el -5 negativo) → confianza 1.0
        var ageValues = new[] { "28", "35", "-5", "30", "25", "22", "999", "31", "27", "33" };

        var ageResult = InferAndLog("Age column", ageValues);
        ageResult.DataType.Should().Be(DataType.Integer);
        ageResult.Confidence.Should().Be(1.0);

        // ── Columna Salary de employees.xlsx ──
        // Todos enteros (incluyendo el -9000 negativo) → confianza 1.0
        var salaryValues = new[] { "55000", "72000", "48000", "-9000", "61000", "43000", "95000", "58000", "67000", "71000" };

        var salaryResult = InferAndLog("Salary column", salaryValues);
        salaryResult.DataType.Should().Be(DataType.Integer);
        salaryResult.Confidence.Should().Be(1.0);
    }
}
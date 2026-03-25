// =============================================================================
// ValidationEngineTests.cs — ExcelInsights.Tests/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO AQUÍ VS ValidationRulesTests?
//
// ValidationRulesTests → cada regla sola, sin engine
//   "¿Esta regla detecta este error específico?"
//
// ValidationEngineTests → el engine con todas las reglas juntas
//   "¿El engine orquesta bien? ¿Acumula todos los errores de una fila?"
//   "¿Una fila con múltiples errores produce múltiples ValidationError?"
//
// SETUP DEL ENGINE:
// El engine recibe IEnumerable<IValidationRule> en el constructor.
// En los tests lo instanciamos directamente con las 4 reglas reales —
// no necesitamos DI container para esto.
// Esto verifica que el engine funciona con las reglas reales,
// igual que en producción.
//
// HELPER BuildRow:
// Crea un RowData con las celdas que necesita cada test.
// Evita repetir la construcción del diccionario en cada test.
// =============================================================================

using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsights.Enums.Entities;
using ExcelInsights.Infrastructure.Excel;
using ExcelInsights.Infrastructure.Excel.Rules;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;
using FluentAssertions;
using Xunit.Abstractions;

namespace ExcelInsights.Tests;

public class ValidationEngineTests
{
    private readonly ITestOutputHelper _output;

    // Engine construido con las 4 reglas reales — igual que en producción
    private readonly ValidationEngine _engine;

    public ValidationEngineTests(ITestOutputHelper output)
    {
        _output = output;

        // Instanciamos el engine con todas las reglas reales.
        // En producción el DI container hace esto automáticamente.
        // En los tests lo hacemos explícitamente para tener control total.
        _engine = new ValidationEngine(new IValidationRule[]
        {
            new EmptyCellRule(),
            new NegativeNumberRule(),
            new InvalidEmailRule(),
            new InvalidDateRule()
        });
    }

    // -------------------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------------------

    private static ColumnDefinition BuildColumn(string name, DataType type) =>
        new ColumnDefinition { Name = name }
            .Also(c => c.SetInferredType(new InferredType(type, 1.0)));

    private static RowData BuildRow(int rowIndex, Dictionary<string, string> cells) =>
        new RowData { RowIndex = rowIndex, Cells = cells };

    private void LogValidationResult(string testName, RowData row, IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToList();
        _output.WriteLine($"[{testName}]");
        _output.WriteLine($"  Fila {row.RowIndex}: {row.Cells.Count} celdas");

        foreach (var cell in row.Cells)
            _output.WriteLine($"    {cell.Key}: \"{cell.Value}\"");

        _output.WriteLine($"  Errores detectados: {errorList.Count}");

        foreach (var error in errorList)
            _output.WriteLine($"    → [{error.Severity}] {error.ColumnName}: {error.Message}");

        _output.WriteLine("");
    }

    // =========================================================================
    // TESTS DEL ENGINE
    // =========================================================================

    [Fact]
    public void Validate_ValidRow_ReturnsNoErrors()
    {
        var columns = new[]
        {
            BuildColumn("Email",  DataType.Email),
            BuildColumn("Age",    DataType.Integer),
            BuildColumn("Salary", DataType.Integer)
        };

        var row = BuildRow(1, new Dictionary<string, string>
        {
            { "Email",  "alice@company.com" },
            { "Age",    "28" },
            { "Salary", "55000" }
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_ValidRow_ReturnsNoErrors), row, errors);

        // Una fila completamente válida no debe producir ningún error
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsOneError()
    {
        var columns = new[]
        {
            BuildColumn("Email", DataType.Email),
            BuildColumn("Age",   DataType.Integer)
        };

        var row = BuildRow(3, new Dictionary<string, string>
        {
            { "Email", "carlos@ruiz" },  // inválido — sin dominio con punto
            { "Age",   "25" }
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_InvalidEmail_ReturnsOneError), row, errors);

        errors.Should().HaveCount(1);
        errors[0].ColumnName.Should().Be("Email");
        errors[0].Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public void Validate_NegativeAge_ReturnsOneError()
    {
        var columns = new[]
        {
            BuildColumn("Email", DataType.Email),
            BuildColumn("Age",   DataType.Integer)
        };

        var row = BuildRow(3, new Dictionary<string, string>
        {
            { "Email", "carlos@company.com" },
            { "Age",   "-5" }  // negativo — inválido para una edad
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_NegativeAge_ReturnsOneError), row, errors);

        errors.Should().HaveCount(1);
        errors[0].ColumnName.Should().Be("Age");
        errors[0].Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public void Validate_EmptyCell_ReturnsOneWarning()
    {
        var columns = new[]
        {
            BuildColumn("Email", DataType.Email),
            BuildColumn("Age",   DataType.Integer)
        };

        var row = BuildRow(5, new Dictionary<string, string>
        {
            { "Email", "" },    // vacío — Warning
            { "Age",   "25" }
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_EmptyCell_ReturnsOneWarning), row, errors);

        // Celda vacía es Warning, no Error
        errors.Should().HaveCount(1);
        errors[0].Severity.Should().Be(ErrorSeverity.Warning);
        errors[0].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Esta fila tiene TRES problemas simultáneos:
        // email inválido + edad negativa + celda de departamento vacía
        var columns = new[]
        {
            BuildColumn("Email",      DataType.Email),
            BuildColumn("Age",        DataType.Integer),
            BuildColumn("Department", DataType.FreeText)
        };

        var row = BuildRow(3, new Dictionary<string, string>
        {
            { "Email",      "carlos@ruiz" },  // Error: email inválido
            { "Age",        "-5" },            // Error: negativo
            { "Department", "" }               // Warning: vacío
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_MultipleErrors_ReturnsAllErrors), row, errors);

        // Deben detectarse los 3 problemas — el engine no para en el primero
        errors.Should().HaveCount(3);

        errors.Should().Contain(e => e.ColumnName == "Email"      && e.Severity == ErrorSeverity.Error);
        errors.Should().Contain(e => e.ColumnName == "Age"        && e.Severity == ErrorSeverity.Error);
        errors.Should().Contain(e => e.ColumnName == "Department" && e.Severity == ErrorSeverity.Warning);
    }

    [Fact]
    public void Validate_InvalidDate_ReturnsError()
    {
        var columns = new[]
        {
            BuildColumn("ExpiryDate", DataType.Date)
        };

        var row = BuildRow(6, new Dictionary<string, string>
        {
            { "ExpiryDate", "not-a-date" }
        });

        var errors = _engine.Validate(row, columns).ToList();
        LogValidationResult(nameof(Validate_InvalidDate_ReturnsError), row, errors);

        errors.Should().HaveCount(1);
        errors[0].ColumnName.Should().Be("ExpiryDate");
        errors[0].Severity.Should().Be(ErrorSeverity.Error);
    }

    // =========================================================================
    // TEST END-TO-END — simula el flujo completo con datos de employees.xlsx
    // =========================================================================

    [Fact]
    public void Validate_EmployeesXlsx_DetectsKnownErrors()
    {
        // Columnas con sus tipos tal como los devolvería el inferidor
        var columns = new[]
        {
            BuildColumn("Name",       DataType.FreeText),
            BuildColumn("Email",      DataType.Email),
            BuildColumn("Age",        DataType.Integer),
            BuildColumn("Salary",     DataType.Integer),
            BuildColumn("HireDate",   DataType.Date),
            BuildColumn("Department", DataType.FreeText)
        };

        // ── Fila 1 — válida ──────────────────────────────────────────────────
        var row1 = BuildRow(2, new Dictionary<string, string>
        {
            { "Name",       "Alice Johnson" },
            { "Email",      "alice@company.com" },
            { "Age",        "28" },
            { "Salary",     "55000" },
            { "HireDate",   "2021-03-15" },
            { "Department", "Engineering" }
        });

        var errorsRow1 = _engine.Validate(row1, columns).ToList();
        LogValidationResult("Row 1 (Alice — válida)", row1, errorsRow1);
        errorsRow1.Should().BeEmpty("Alice es una fila completamente válida");

        // ── Fila 3 — email inválido + edad negativa ──────────────────────────
        var row3 = BuildRow(4, new Dictionary<string, string>
        {
            { "Name",       "Carlos Ruiz" },
            { "Email",      "carlos@ruiz" },    // inválido
            { "Age",        "-5" },              // negativo
            { "Salary",     "48000" },
            { "HireDate",   "2022-11-20" },
            { "Department", "Sales" }
        });

        var errorsRow3 = _engine.Validate(row3, columns).ToList();
        LogValidationResult("Row 3 (Carlos — email inválido + edad negativa)", row3, errorsRow3);

        errorsRow3.Should().HaveCount(2);
        errorsRow3.Should().Contain(e => e.ColumnName == "Email" && e.Severity == ErrorSeverity.Error);
        errorsRow3.Should().Contain(e => e.ColumnName == "Age"   && e.Severity == ErrorSeverity.Error);

        // ── Fila 4 — salario negativo ────────────────────────────────────────
        var row4 = BuildRow(5, new Dictionary<string, string>
        {
            { "Name",       "Diana Lee" },
            { "Email",      "diana@company.com" },
            { "Age",        "30" },
            { "Salary",     "-9000" },           // negativo
            { "HireDate",   "2023-01-10" },
            { "Department", "HR" }
        });

        var errorsRow4 = _engine.Validate(row4, columns).ToList();
        LogValidationResult("Row 4 (Diana — salario negativo)", row4, errorsRow4);

        errorsRow4.Should().HaveCount(1);
        errorsRow4.Should().Contain(e => e.ColumnName == "Salary" && e.Severity == ErrorSeverity.Error);
    }
}

// =============================================================================
// EXTENSION METHOD — Also()
// =============================================================================
//
// ¿POR QUÉ ESTE MÉTODO?
// C# no tiene una forma nativa de encadenar operaciones de configuración
// sobre un objeto y devolverlo en la misma expresión.
// Also() permite escribir:
//   new ColumnDefinition { Name = "Email" }.Also(c => c.SetInferredType(...))
// en lugar de:
//   var c = new ColumnDefinition { Name = "Email" };
//   c.SetInferredType(...);
//   return c;
// Es un helper de conveniencia solo para los tests — no va en producción.
// =============================================================================

public static class ObjectExtensions
{
    public static T Also<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }
}
// =============================================================================
// ValidationRulesTests.cs — ExcelInsights.Tests/Rules/
// =============================================================================
//
// ¿QUÉ ESTAMOS TESTEANDO?
// Cada regla en aislamiento total. Una regla recibe tres datos:
//   - el valor de la celda (string)
//   - la ColumnDefinition con su tipo inferido
//   - el índice de la fila
// y devuelve ValidationError? (null si no hay error).
//
// ¿POR QUÉ TESTEAR CADA REGLA POR SEPARADO?
// Si solo testeamos el ValidationEngine completo y un test falla,
// no sabemos qué regla falló. Testeando cada regla sola, el test
// que falla señala exactamente el problema.
//
// HELPER BuildColumn(DataType):
// Crea una ColumnDefinition con el tipo inferido que necesita cada test.
// Evita repetir la construcción en cada test — un cambio en ColumnDefinition
// solo requiere actualizar el helper.
// =============================================================================

using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsights.Enums.Entities;
using ExcelInsights.Infrastructure.Excel.Rules;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;
using FluentAssertions;
using Xunit.Abstractions;

namespace ExcelInsights.Tests.Rules;

public class ValidationRulesTests
{
    private readonly ITestOutputHelper _output;

    public ValidationRulesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // -------------------------------------------------------------------------
    // HELPER — construye una ColumnDefinition con el tipo inferido dado
    // -------------------------------------------------------------------------

    /// <summary>
    /// Crea una ColumnDefinition lista para usar en los tests.
    /// InferredType se setea directamente para simular el resultado
    /// del ColumnInferrerService sin tener que ejecutarlo realmente.
    /// </summary>
    private static ColumnDefinition BuildColumn(string name, DataType type)
    {
        var column = new ColumnDefinition { Name = name };
        column.SetInferredType(new InferredType(type, 1.0));
        return column;
    }

    /// <summary>
    /// Loggea la entrada y salida de una regla para debug cuando falla.
    /// </summary>
    private void LogRuleResult(
        string ruleName,
        string value,
        DataType columnType,
        ValidationError? result)
    {
        _output.WriteLine($"[{ruleName}]");
        _output.WriteLine($"  Valor: \"{value}\" | Tipo columna: {columnType}");
        _output.WriteLine($"  Resultado: {(result == null ? "null (sin error)" : $"Error — {result.Message} [{result.Severity}]")}");
        _output.WriteLine("");
    }

    // =========================================================================
    // NEGATIVE NUMBER RULE
    // =========================================================================

    [Fact]
    public void NegativeNumberRule_NegativeInteger_ReturnsError()
    {
        var rule   = new NegativeNumberRule();
        var column = BuildColumn("Age", DataType.Integer);

        var result = rule.Validate("-5", column, rowIndex: 3);
        LogRuleResult(nameof(NegativeNumberRule_NegativeInteger_ReturnsError), "-5", DataType.Integer, result);

        // Un entero negativo en una columna Integer es siempre un Error
        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Error);
        result.RowIndex.Should().Be(3);
        result.ColumnName.Should().Be("Age");
    }

    [Fact]
    public void NegativeNumberRule_PositiveInteger_ReturnsNull()
    {
        var rule   = new NegativeNumberRule();
        var column = BuildColumn("Age", DataType.Integer);

        var result = rule.Validate("28", column, rowIndex: 2);
        LogRuleResult(nameof(NegativeNumberRule_PositiveInteger_ReturnsNull), "28", DataType.Integer, result);

        // Un entero positivo no es un error — la regla no aplica
        result.Should().BeNull();
    }

    [Fact]
    public void NegativeNumberRule_NegativeDecimal_ReturnsError()
    {
        var rule   = new NegativeNumberRule();
        var column = BuildColumn("Price", DataType.Decimal);

        var result = rule.Validate("-29.99", column, rowIndex: 2);
        LogRuleResult(nameof(NegativeNumberRule_NegativeDecimal_ReturnsError), "-29.99", DataType.Decimal, result);

        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public void NegativeNumberRule_NegativeValueInEmailColumn_ReturnsNull()
    {
        var rule   = new NegativeNumberRule();
        // Columna de tipo Email — la regla no debe aplicar aquí
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("-5", column, rowIndex: 3);
        LogRuleResult(nameof(NegativeNumberRule_NegativeValueInEmailColumn_ReturnsNull), "-5", DataType.Email, result);

        // Aunque el valor parece negativo, la columna es Email — no aplica
        result.Should().BeNull();
    }

    [Fact]
    public void NegativeNumberRule_Zero_ReturnsNull()
    {
        var rule   = new NegativeNumberRule();
        var column = BuildColumn("Stock", DataType.Integer);

        var result = rule.Validate("0", column, rowIndex: 1);
        LogRuleResult(nameof(NegativeNumberRule_Zero_ReturnsNull), "0", DataType.Integer, result);

        // Cero no es negativo — no es un error
        result.Should().BeNull();
    }

    // =========================================================================
    // INVALID EMAIL RULE
    // =========================================================================

    [Fact]
    public void InvalidEmailRule_InvalidEmail_ReturnsError()
    {
        var rule   = new InvalidEmailRule();
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("carlos@ruiz", column, rowIndex: 3);
        LogRuleResult(nameof(InvalidEmailRule_InvalidEmail_ReturnsError), "carlos@ruiz", DataType.Email, result);

        // "carlos@ruiz" no tiene dominio con punto — es inválido
        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public void InvalidEmailRule_ValidEmail_ReturnsNull()
    {
        var rule   = new InvalidEmailRule();
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("alice@company.com", column, rowIndex: 1);
        LogRuleResult(nameof(InvalidEmailRule_ValidEmail_ReturnsNull), "alice@company.com", DataType.Email, result);

        result.Should().BeNull();
    }

    [Fact]
    public void InvalidEmailRule_EmailValueInIntegerColumn_ReturnsNull()
    {
        var rule   = new InvalidEmailRule();
        // Columna de tipo Integer — la regla no debe aplicar aunque el valor parezca email
        var column = BuildColumn("Age", DataType.Integer);

        var result = rule.Validate("notanemail", column, rowIndex: 5);
        LogRuleResult(nameof(InvalidEmailRule_EmailValueInIntegerColumn_ReturnsNull), "notanemail", DataType.Integer, result);

        // La columna no es Email — la regla no aplica
        result.Should().BeNull();
    }

    [Fact]
    public void InvalidEmailRule_EmptyValue_ReturnsNull()
    {
        var rule   = new InvalidEmailRule();
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("", column, rowIndex: 5);
        LogRuleResult(nameof(InvalidEmailRule_EmptyValue_ReturnsNull), "", DataType.Email, result);

        // Celda vacía no es responsabilidad de InvalidEmailRule
        // sino de EmptyCellRule — cada regla tiene una sola responsabilidad
        result.Should().BeNull();
    }

    [Fact]
    public void InvalidEmailRule_NoAtSign_ReturnsError()
    {
        var rule   = new InvalidEmailRule();
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("notanemail", column, rowIndex: 8);
        LogRuleResult(nameof(InvalidEmailRule_NoAtSign_ReturnsError), "notanemail", DataType.Email, result);

        result.Should().NotBeNull();
        result!.Message.Should().Contain("notanemail");
    }

    // =========================================================================
    // EMPTY CELL RULE
    // =========================================================================

    [Fact]
    public void EmptyCellRule_EmptyString_ReturnsWarning()
    {
        var rule   = new EmptyCellRule();
        var column = BuildColumn("Email", DataType.Email);

        var result = rule.Validate("", column, rowIndex: 5);
        LogRuleResult(nameof(EmptyCellRule_EmptyString_ReturnsWarning), "", DataType.Email, result);

        // Una celda vacía es Warning, no Error — puede ser intencional
        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void EmptyCellRule_WhitespaceOnly_ReturnsWarning()
    {
        var rule   = new EmptyCellRule();
        var column = BuildColumn("Name", DataType.FreeText);

        var result = rule.Validate("   ", column, rowIndex: 2);
        LogRuleResult(nameof(EmptyCellRule_WhitespaceOnly_ReturnsWarning), "   ", DataType.FreeText, result);

        // Solo espacios se trata igual que vacío
        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void EmptyCellRule_ValuePresent_ReturnsNull()
    {
        var rule   = new EmptyCellRule();
        var column = BuildColumn("Name", DataType.FreeText);

        var result = rule.Validate("Alice Johnson", column, rowIndex: 1);
        LogRuleResult(nameof(EmptyCellRule_ValuePresent_ReturnsNull), "Alice Johnson", DataType.FreeText, result);

        result.Should().BeNull();
    }

    [Fact]
    public void EmptyCellRule_AppliesToAnyColumnType()
    {
        var rule = new EmptyCellRule();

        // La regla aplica a todos los tipos — verificamos los más representativos
        var types = new[] { DataType.Integer, DataType.Email, DataType.Date, DataType.Boolean };

        foreach (var type in types)
        {
            var column = BuildColumn("TestColumn", type);
            var result = rule.Validate("", column, rowIndex: 1);

            _output.WriteLine($"  EmptyCellRule en columna {type}: {(result == null ? "null" : result.Severity.ToString())}");

            // Sin importar el tipo, celda vacía siempre es Warning
            result.Should().NotBeNull($"EmptyCellRule debe aplicar a columnas de tipo {type}");
            result!.Severity.Should().Be(ErrorSeverity.Warning);
        }
    }

    // =========================================================================
    // INVALID DATE RULE
    // =========================================================================

    [Fact]
    public void InvalidDateRule_InvalidFormat_ReturnsError()
    {
        var rule   = new InvalidDateRule();
        var column = BuildColumn("ExpiryDate", DataType.Date);

        var result = rule.Validate("not-a-date", column, rowIndex: 6);
        LogRuleResult(nameof(InvalidDateRule_InvalidFormat_ReturnsError), "not-a-date", DataType.Date, result);

        result.Should().NotBeNull();
        result!.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Fact]
    public void InvalidDateRule_ValidDate_ReturnsNull()
    {
        var rule   = new InvalidDateRule();
        var column = BuildColumn("HireDate", DataType.Date);

        var result = rule.Validate("2024-03-15", column, rowIndex: 1);
        LogRuleResult(nameof(InvalidDateRule_ValidDate_ReturnsNull), "2024-03-15", DataType.Date, result);

        result.Should().BeNull();
    }

    [Fact]
    public void InvalidDateRule_EmptyValue_ReturnsNull()
    {
        var rule   = new InvalidDateRule();
        var column = BuildColumn("HireDate", DataType.Date);

        var result = rule.Validate("", column, rowIndex: 3);
        LogRuleResult(nameof(InvalidDateRule_EmptyValue_ReturnsNull), "", DataType.Date, result);

        // Celda vacía no es responsabilidad de InvalidDateRule — es de EmptyCellRule
        result.Should().BeNull();
    }

    [Fact]
    public void InvalidDateRule_DateInNonDateColumn_ReturnsNull()
    {
        var rule   = new InvalidDateRule();
        var column = BuildColumn("Age", DataType.Integer);

        var result = rule.Validate("not-a-date", column, rowIndex: 1);
        LogRuleResult(nameof(InvalidDateRule_DateInNonDateColumn_ReturnsNull), "not-a-date", DataType.Integer, result);

        // La columna no es Date — la regla no aplica
        result.Should().BeNull();
    }
}
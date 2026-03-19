using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel;

/// <summary>
/// Implementación del motor de validación.
/// STUB — Issue 1. La lógica real se implementa en Issue 4.
/// </summary>
public class ValidationEngine : IValidationEngine
{

    IEnumerable<ValidationError> IValidationEngine.Validate(RowData row, IEnumerable<ColumnDefinition> columns)
    {
        // Issue 4: aquí irán las reglas de validación por tipo
        // (NegativeNumberRule, InvalidEmailRule, NullValueRule, etc.)
        throw new NotImplementedException("Implementar en Issue 4.");
    }
}

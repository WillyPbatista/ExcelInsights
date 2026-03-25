using ExcelInsights.Domain.Entities;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel.Rules
{
    public class EmptyCellRule : IValidationRule
    {
        public ValidationError? Validate(string value, ColumnDefinition column, int rowIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ValidationError
                {
                    RowIndex = rowIndex,
                    ColumnName = column.Name,
                    Message = $"Empty cell found in column: {column.Name}",
                    Severity = ErrorSeverity.Warning
                };
            }

            return null;
        }
    }
}
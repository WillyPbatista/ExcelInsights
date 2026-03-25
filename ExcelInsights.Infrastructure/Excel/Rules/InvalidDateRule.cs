using ExcelInsights.Domain.Entities;
using ExcelInsights.Enums.Entities;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel.Rules
{
    public class InvalidDateRule : IValidationRule
    {

        public ValidationError? Validate(string value, ColumnDefinition column, int rowIndex)
        {
            if (column.InferredType.DataType != DataType.Date)
                return null;
                
            if (!string.IsNullOrEmpty(value) && !DateTime.TryParse(value, out _))
            {
                return new ValidationError
                {
                    RowIndex = rowIndex,
                    ColumnName = column.Name,
                    Message = $"Invalid date format: {value}",
                    Severity = ErrorSeverity.Error
                };
            }

            return null;
        }
    }
}
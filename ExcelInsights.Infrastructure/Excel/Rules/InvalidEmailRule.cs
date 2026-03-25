using ExcelInsights.Domain.Entities;
using ExcelInsights.Enums.Entities;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel.Rules
{
    public class InvalidEmailRule : IValidationRule
    {
        public ValidationError? Validate(string value, ColumnDefinition column, int rowIndex)
        {
            if (column.InferredType.DataType != DataType.Email)
                return null;



            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
            {
                return new ValidationError
                {
                    RowIndex = rowIndex,
                    ColumnName = column.Name,
                    Message = $"Invalid email format: {value}",
                    Severity = ErrorSeverity.Error
                };
            }

            return null;
        }

        private static bool IsValidEmail(string value)
        {
            var parts = value.Split('@');
            if (parts.Length != 2) return false;
            if (parts[0].Length == 0) return false;
            var dotIndex = parts[1].LastIndexOf('.');
            if (dotIndex < 1) return false;
            if (dotIndex == parts[1].Length - 1) return false;
            return true;
        }
    }
}
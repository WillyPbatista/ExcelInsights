using ExcelInsights.Domain.Entities;
using ExcelInsights.Enums.Entities;
using ExcelInsigths.Domain.Enunms;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel.Rules
{
    public class NegativeNumberRule : IValidationRule
    {
        public ValidationError? Validate(string value, ColumnDefinition column, int rowIndex)
        {
            if (column.InferredType.DataType != DataType.Integer &&
                column.InferredType.DataType != DataType.Decimal)
                return null;


            if (decimal.TryParse(value, out decimal number))
            {
                if (number < 0)
                {
                    return new ValidationError
                    {
                        RowIndex = rowIndex,
                        ColumnName = column.Name,
                        Message = $"Negative number found: {value}",
                        Severity = ErrorSeverity.Error
                    };
                }
            }

            return null;
        }
    }

}

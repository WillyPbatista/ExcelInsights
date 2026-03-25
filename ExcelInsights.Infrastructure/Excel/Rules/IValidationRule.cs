using ExcelInsights.Domain.Entities;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel.Rules
{
    public interface IValidationRule
    {
        ValidationError? Validate(string value, ColumnDefinition column, int rowIndex);
    }
}
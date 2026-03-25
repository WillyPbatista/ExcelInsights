using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.Entities;
using ExcelInsights.Infrastructure.Excel.Rules;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel;
public class ValidationEngine : IValidationEngine
{
    private readonly IEnumerable<IValidationRule> _rules;
    public ValidationEngine(IEnumerable<IValidationRule> rules)
    {
        _rules = rules;

    }


    public IEnumerable<ValidationError> Validate(RowData row, IEnumerable<ColumnDefinition> columns)
    {
        var errors = new List<ValidationError>();
        foreach (var column in columns)
        {
            var cellValue = row.Cells.TryGetValue(column.Name, out var value) ? value : string.Empty;

            foreach (var rule in _rules)
            {
                var error = rule.Validate(cellValue, column, row.RowIndex);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
        }
        return errors;

    }
}
using System.Collections.Generic;
using ExcelInsights.Domain.Entities;
using ExcelInsigths.Domain.ValueObjects;

namespace ExcelInsights.Application.Contracts
{
    public interface IValidationEngine
    {
        IEnumerable<ValidationError> Validate(RowData row, IEnumerable<ColumnDefinition> columns);
    }
}

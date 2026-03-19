using System.Collections.Generic;
using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Domain.Entities
{
    public class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;

        public InferredType InferredType { get; set; } = new InferredType();

        public ColumnStats Stats { get; set; } = new ColumnStats();

        public List<string> Values { get; set; } = new List<string>();
    }
}

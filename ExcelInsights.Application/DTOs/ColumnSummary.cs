using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Application.DTOs
{
    public class ColumnSummary
    {
        public string Name { get; set; } = string.Empty;
        public string InferredType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public ColumnStats Stats { get; set; } = new ColumnStats();
    }
}

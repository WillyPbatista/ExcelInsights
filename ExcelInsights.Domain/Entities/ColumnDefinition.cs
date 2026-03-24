using ExcelInsights.Enums.Entities;
using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Domain.Entities;

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;

    public InferredType InferredType { get; private set; } = InferredType.Unknown();

    public ColumnStats Stats { get; set; } = new ColumnStats();

    public List<string> Values { get; set; } = new List<string>();

    public void SetInferredType(InferredType inferredType)
    {
        InferredType = inferredType;
    }
}

using ExcelInsights.Enums.Entities;

namespace ExcelInsights.Domain.ValueObjects;

public record InferredType(DataType DataType, double Confidence)
{
    public static InferredType Unknown()
        => new InferredType(DataType.Unknown, 0.0);
}
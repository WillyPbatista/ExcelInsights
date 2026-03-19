using ExcelInsigths.Enums.Entities;

namespace ExcelInsights.Domain.ValueObjects
{
    public class InferredType
    {
        public DataType DataType { get; set; }
        public double Confidence { get; set; }
    }
}
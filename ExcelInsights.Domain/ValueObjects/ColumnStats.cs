namespace ExcelInsights.Domain.ValueObjects
{
    public class ColumnStats
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public double Average { get; set; }
        public int NullCount { get; set; }
        public int UniqueCount { get; set; }
    }
}
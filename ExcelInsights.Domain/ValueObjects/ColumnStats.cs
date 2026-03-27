namespace ExcelInsights.Domain.ValueObjects
{
    public class ColumnStats
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public double? Average { get; set; }
        public int NullCount { get; set; }
        public int UniqueCount { get; set; }
    }
}
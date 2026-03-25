namespace ExcelInsights.Application.DTOs
{
    public class ColumnStatsDto
    {
        public double? Average { get; set; }
        public string? Min { get; set; }
        public string? Max { get; set; }
        public int NullCount { get; set; }
        public int UniqueCount { get; set; }
    }
}

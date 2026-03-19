namespace ExcelInsights.Application.DTOs
{
    public class RowErrorDto
    {
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }
}

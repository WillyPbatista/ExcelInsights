using System.Collections.Generic;

namespace ExcelInsights.Application.DTOs
{
    public class AnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<ColumnSummary> Columns { get; set; } = new List<ColumnSummary>();
        public List<RowErrorDto> Errors { get; set; } = new List<RowErrorDto>();
    }
}

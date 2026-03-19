using System.Collections.Generic;

namespace ExcelInsights.Domain.Entities
{
    public class ExcelFile
    {
        public string FileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }

        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();

        public List<RowData> Rows { get; set; } = new List<RowData>();
    }
}

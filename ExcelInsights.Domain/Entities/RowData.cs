using System.Collections.Generic;

namespace ExcelInsights.Domain.Entities
{
    public class RowData
    {
        public int RowIndex { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new Dictionary<string, string>();
    }
}

using ExcelInsigths.Domain.Enunms;

namespace ExcelInsigths.Domain.ValueObjects
{
    public class ValidationError
    {
        public int RowIndex { get; set; }
        public string ColumnName { get; set; }
        public String Message { get; set; }
        public ErrorSeverity Severity { get; set; }

    }
}
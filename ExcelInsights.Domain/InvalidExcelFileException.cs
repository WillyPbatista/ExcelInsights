namespace ExcelInsights.Domain;

public class InvalidExcelFileException : ArgumentException
{
    public InvalidExcelFileException(string message) : base(message)
    {
    }

    public InvalidExcelFileException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
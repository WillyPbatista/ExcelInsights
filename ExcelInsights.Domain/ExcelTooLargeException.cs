namespace ExcelInsights.Domain;

public class ExcelTooLargeException : ArgumentException
{
    public ExcelTooLargeException(string message) : base(message)
    {
    }
}
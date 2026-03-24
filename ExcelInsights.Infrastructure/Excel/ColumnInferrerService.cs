using System.Globalization;
using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.ValueObjects;
using ExcelInsights.Enums.Entities;

namespace ExcelInsights.Infrastructure.Excel;

public class ColumnInferrerService : IColumnInferrer
{
    public InferredType Infer(IEnumerable<string> values)
    {
        var nonEmpty = new List<string>();
        int isBoolean = 0;
        int isDecimal = 0;
        int isInteger = 0;
        int isDate = 0;
        int isEmail = 0;
        const double MinConfidenceThreshold = 0.6;

        foreach (var value in values)
        {
            if (value != null && value.Trim() != string.Empty)
                nonEmpty.Add(value);
        }
        if (!nonEmpty.Any())
            return InferredType.Unknown();
            
        foreach (var value in nonEmpty)
        {
            if (IsBoolean(value))
                isBoolean++;
            else if (long.TryParse(value, out _))
            {
                isInteger++;
                isDecimal++;
            }
            else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                isDecimal++;
            }
            else if (DateTime.TryParse(value, out _))
                isDate++;
            else if (IsEmail(value))
                isEmail++;
        }

        var winningType = DetermineDataType(isBoolean, isDecimal, isInteger, isDate, isEmail);
        var winningVotes = winningType switch
        {
            DataType.Boolean => isBoolean,
            DataType.Integer => isInteger,
            DataType.Decimal => isDecimal,
            DataType.Date => isDate,
            DataType.Email => isEmail,
            _ => 0
        };

        var confidence = CalculateConfidence(nonEmpty.Count, winningVotes);


        if (confidence < MinConfidenceThreshold)
            return new InferredType(DataType.FreeText, confidence);

        return new InferredType(DataType: winningType,
        Confidence: confidence);
    }

    private static DataType DetermineDataType(int isBoolean, int isDecimal, int isInteger, int isDate, int isEmail)
    {
        int max = new[] { isBoolean, isInteger, isDecimal, isDate, isEmail }.Max();

        if (max == 0) return DataType.Unknown;

        if (isBoolean == max) return DataType.Boolean;
        if (isInteger == max) return DataType.Integer;
        if (isDecimal == max) return DataType.Decimal;
        if (isDate == max) return DataType.Date;
        if (isEmail == max) return DataType.Email;

        return DataType.FreeText;
    }

    private double CalculateConfidence(int total, int matches)
    {
        if (total == 0) return 0.0;
        return (double)matches / total;
    }

    private static readonly HashSet<string> BooleanValues =
    new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "false", "yes", "no", "si", "sí", "1", "0"
    };

    private static bool IsBoolean(string value)
        => BooleanValues.Contains(value.Trim());

    private static bool IsEmail(string value)
    {
        var parts = value.Split('@');
        if (parts.Length != 2) return false;
        if (parts[0].Length == 0) return false;

        var domain = parts[1];
        var dotIndex = domain.LastIndexOf('.');
        if (dotIndex < 1) return false;
        if (dotIndex == domain.Length - 1) return false;

        return true;
    }
}

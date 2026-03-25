using System.Globalization;
using ExcelInsights.Domain.Entities;
using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Domain.Services
{
    public class StatisticsService
    {
        public static ColumnStats Calculate(ColumnDefinition column)
        {
            var nonEmpty = column.Values.Where(v => !string.IsNullOrEmpty(v)).ToList();
            var nullCount = column.Values.Count - nonEmpty.Count;
            var uniqueCount = nonEmpty.Distinct().Count();


            if (column.InferredType.DataType == Enums.Entities.DataType.Number || column.InferredType.DataType == Enums.Entities.DataType.Decimal)
            {
                var parsed = new List<decimal>();

                foreach (var value in nonEmpty)
                {
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                        parsed.Add(number);
                }
                return new ColumnStats
                {
                    Min = parsed.Min(),
                    Max = parsed.Max(),
                    Average = parsed.Count > 0 ? (double)parsed.Average() : null,
                    NullCount = nullCount,
                    UniqueCount = uniqueCount
                };
            }
            else
            {
                return new ColumnStats
                {
                    Min = null,
                    Max = null,
                    Average = null,
                    NullCount = nullCount,
                    UniqueCount = uniqueCount
                };
            }
        }
    }
}

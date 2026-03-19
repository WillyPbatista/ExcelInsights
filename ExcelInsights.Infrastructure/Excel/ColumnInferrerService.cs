using ExcelInsights.Application.Contracts;
using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Infrastructure.Excel;

/// <summary>
/// Implementación del inferidor de tipos de columna.
/// STUB — Issue 1. La lógica real se implementa en Issue 3.
/// </summary>
public class ColumnInferrerService : IColumnInferrer
{
    public InferredType Infer(IEnumerable<string> values)
    {
        // Issue 3: aquí irá la lógica de votación para detectar
        // si la columna es Email, Integer, Date, etc.
        throw new NotImplementedException("Implementar en Issue 3.");
    }
}

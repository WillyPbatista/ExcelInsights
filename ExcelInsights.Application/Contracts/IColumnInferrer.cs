using System.Collections.Generic;
using ExcelInsights.Domain.ValueObjects;

namespace ExcelInsights.Application.Contracts
{
    public interface IColumnInferrer
    {
        InferredType Infer(IEnumerable<string> values);
    }
}

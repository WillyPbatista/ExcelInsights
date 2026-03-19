using ExcelInsights.Application.DTOs;

namespace ExcelInsights.Application.Contracts
{
    public interface IPdfGenerator
    {
        Task<byte[]> GenerateAsync(AnalysisResult result);
    }
}

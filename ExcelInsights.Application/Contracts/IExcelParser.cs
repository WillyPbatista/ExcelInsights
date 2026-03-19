using System.IO;
using System.Threading.Tasks;
using ExcelInsights.Domain.Entities;

namespace ExcelInsights.Application.Contracts
{
    public interface IExcelParser
    {
        Task<ExcelFile> ParseAsync(Stream fileStream, string fileName);
    }
}

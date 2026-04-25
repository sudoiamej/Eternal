using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Security
{
    public interface IFileForensicsService
    {
        Task<FileForensicResult?> AnalyzeFileAsync(string filePath);
    }
}

using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IDismService
    {
        Task<WimFileDetails?> GetImageInfoAsync(string filePath);
    }
}

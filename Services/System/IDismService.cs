using System;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IDismService
    {
        Task<WimFileDetails?> GetImageInfoAsync(string filePath);
        Task<bool> InjectDriversAsync(string imagePath, string driverPath, bool forceUnsigned, Action<string> progressCallback);
        Task<bool> RestoreHealthFromSourceAsync(string targetPath, string sourceWimPath, int imageIndex, bool isOnline, Action<string> progressCallback);
    }
}

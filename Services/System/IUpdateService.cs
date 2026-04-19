using System;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public record UpdateInfo(bool IsUpdateAvailable, string NewVersion, string Changelog, string DownloadUrl);

    public interface IUpdateService
    {
        Task<UpdateInfo> CheckForUpdatesAsync();
        Task<bool> DownloadUpdateAsync(string downloadUrl, IProgress<double> progress);
        void ApplyUpdateAndRestart();
    }
}
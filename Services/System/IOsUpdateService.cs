using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IOsUpdateService
    {
        Task<List<WindowsUpdateItem>> GetAvailableUpdatesAsync();
        Task<List<WindowsUpdateItem>> GetInstalledUpdatesAsync();
        Task<bool> PauseUpdatesAsync(int days);
        Task<bool> ResumeUpdatesAsync();
        Task<(bool IsPaused, DateTime? ResumeDate)> GetPauseStatusAsync();
        Task<bool> InstallUpdatesAsync(List<string> updateIds);
        Task<bool> DownloadAndInstallUpdatesAsync(List<string> updateIds, IProgress<double> progress);
        Task<bool> IsRebootRequiredAsync();
        Task ClearRebootFlagAsync();
        Task RebootSystemAsync();
        Task<WindowsLifecycleInfo> GetWindowsLifecycleInfoAsync();
    }
}

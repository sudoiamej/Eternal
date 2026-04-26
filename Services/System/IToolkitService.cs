using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IToolkitService
    {
        Task<bool> FlushDnsAsync();
        Task<long> ClearTempFilesAsync();
        Task<bool> RebuildIconCacheAsync();
        Task<bool> ResetNetworkStackAsync();
        Task<bool> RunSfcScanAsync();
        Task<bool> RunDismRepairAsync();
        Task<bool> ResetWindowsUpdateAsync();
        Task<bool> ClearEventLogsAsync();
        Task<bool> OptimizeBootPerformanceAsync();
        Task<string?> DetectOfflineWindowsDriveAsync();
        Task<bool> MountOfflineRegistryAsync(string driveLetter);
        Task<bool> UnmountOfflineRegistryAsync();
    }
}

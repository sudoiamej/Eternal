using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Network
{
    public interface INetworkService
    {
        Task<List<NetworkConnection>> GetActiveConnectionsAsync();
        Task<NetworkUsage> GetNetworkUsageAsync(string interfaceName);
        Task<SpeedTestResult> RunSpeedTestAsync(Action<SpeedTestProgress> onProgress);
    }

    public record NetworkUsage(double DownloadMbps, double UploadMbps);

    public record SpeedTestResult(double DownloadSpeedMbps, double UploadSpeedMbps, int PingMs);

    public record SpeedTestProgress(string Phase, int Percentage, double CurrentSpeed);
}
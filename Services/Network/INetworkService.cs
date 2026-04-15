using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Network
{
    public interface INetworkService
    {
        Task<List<NetworkConnection>> GetActiveConnectionsAsync();
        Task<NetworkUsage> GetNetworkUsageAsync(string interfaceName);
    }

    public record NetworkUsage(double DownloadMbps, double UploadMbps);
}
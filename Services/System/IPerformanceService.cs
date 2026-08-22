using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IPerformanceService
    {
        event EventHandler<PerformanceSnapshot> Updated;
        PerformanceSnapshot CurrentSnapshot { get; }
        Task<PerformanceSnapshot> GetCurrentSnapshotAsync();
        void StartPolling();
        void StopPolling();
        void PausePolling();
        void ResumePolling();
    }

    public record PerformanceSnapshot(float CpuUsage, float RamUsage, float DiskUsage, float NetworkUsage);
}

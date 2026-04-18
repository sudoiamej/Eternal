using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class WindowsPerformanceService : IPerformanceService
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _diskCounter;
        private bool _isInitialized = false;
        private global::System.Threading.Timer _pollingTimer;
        private int _isUpdating = 0;

        public event EventHandler<PerformanceSnapshot> Updated;
        public PerformanceSnapshot CurrentSnapshot { get; private set; } = new PerformanceSnapshot(0, 0, 0, 0);

        private void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                _cpuCounter.NextValue();
                _diskCounter.NextValue();
            }
            catch { }
        }

        public void StartPolling()
        {
            if (_pollingTimer != null) return;
            _pollingTimer = new global::System.Threading.Timer(async _ => await DoUpdateAsync(), null, 0, 1000);
        }

        public void StopPolling()
        {
            _pollingTimer?.Dispose();
            _pollingTimer = null;
        }

        private async Task DoUpdateAsync()
        {
            if (Interlocked.CompareExchange(ref _isUpdating, 1, 0) != 0) return;
            try
            {
                var snap = await GetCurrentSnapshotAsync();
                CurrentSnapshot = snap;
                Updated?.Invoke(this, snap);
            }
            finally
            {
                Interlocked.Exchange(ref _isUpdating, 0);
            }
        }

        public Task<PerformanceSnapshot> GetCurrentSnapshotAsync()
        {
            return Task.Run(() =>
            {
                EnsureInitialized();

                float cpu = 0;
                float disk = 0;
                try { cpu = _cpuCounter?.NextValue() ?? 0; } catch { }
                try { disk = _diskCounter?.NextValue() ?? 0; } catch { }

                float ramPercent = 0;
                try
                {
                    using var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        ulong total = global::System.Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                        ulong free = global::System.Convert.ToUInt64(obj["FreePhysicalMemory"]);
                        ramPercent = (float)(1.0 - (double)free / total) * 100;
                        break;
                    }
                }
                catch { }

                return new PerformanceSnapshot(cpu, ramPercent, disk, 0);
            });
        }
    }
}

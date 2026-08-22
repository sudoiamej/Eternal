using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class WindowsPerformanceService : IPerformanceService
    {
        private readonly ISettingsService _settingsService;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _diskCounter;
        private bool _isInitialized = false;
        private global::System.Threading.Timer? _pollingTimer;
        private int _isUpdating = 0;
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        public WindowsPerformanceService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public void Init() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public event EventHandler<PerformanceSnapshot>? Updated;
        public PerformanceSnapshot CurrentSnapshot { get; private set; } = new PerformanceSnapshot(0, 0, 0, 0);

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

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

        public void PausePolling()
        {
            _pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void ResumePolling()
        {
            if (_pollingTimer == null)
            {
                StartPolling();
            }
            else
            {
                _pollingTimer.Change(0, 1000);
            }
        }

        private async Task DoUpdateAsync()
        {
            if (!_settingsService.Current.EnableWmiPolling) return;
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
                    var memStatus = new MEMORYSTATUSEX();
                    memStatus.Init();
                    if (GlobalMemoryStatusEx(ref memStatus))
                    {
                        ramPercent = memStatus.dwMemoryLoad;
                    }
                    else
                    {
                        // Fallback to WMI if P/Invoke fails (rare)
                        using var searcher = CreateSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem");
                        foreach (var obj in searcher.Get())
                        {
                            ulong total = global::System.Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                            ulong free = global::System.Convert.ToUInt64(obj["FreePhysicalMemory"]);
                            ramPercent = (float)(1.0 - (double)free / total) * 100;
                            break;
                        }
                    }
                }
                catch { }

                return new PerformanceSnapshot(cpu, ramPercent, disk, 0);
            });
        }
    }
}

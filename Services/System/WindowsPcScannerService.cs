using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;
using Eternal.Services.Security;
using Eternal.Services.Storage;

namespace Eternal.Services.System
{
    public class WindowsPcScannerService : IPcScannerService
    {
        private readonly ISecurityService _securityService;
        private readonly IStorageService _storageService;
        private readonly IPerformanceService _performanceService;
        private readonly ICreatorService _creatorService;
        private readonly IToolkitService _toolkitService;
        private readonly IBiosService _biosService;
        private readonly IDriversService _driversService;

        public WindowsPcScannerService(
            ISecurityService securityService,
            IStorageService storageService,
            IPerformanceService performanceService,
            ICreatorService creatorService,
            IToolkitService toolkitService,
            IBiosService biosService,
            IDriversService driversService)
        {
            _securityService = securityService;
            _storageService = storageService;
            _performanceService = performanceService;
            _creatorService = creatorService;
            _toolkitService = toolkitService;
            _biosService = biosService;
            _driversService = driversService;
        }

        public async Task<List<ScannerIssue>> RunFullScanAsync(IProgress<int> progress)
        {
            var issuesBag = new ConcurrentBag<ScannerIssue>();
            int completedTasksCount = 0;
            const int totalTasks = 8;

            void ReportProgress()
            {
                int completed = Interlocked.Increment(ref completedTasksCount);
                int currentPercent = (int)((double)completed / totalTasks * 100);
                progress?.Report(currentPercent);
            }

            // Task 1: High-Speed Parallel Storage Audit
            var storageTask = Task.Run(async () =>
            {
                try
                {
                    var disks = await _storageService.GetPhysicalDisksAsync();
                    foreach (var disk in disks)
                    {
                        foreach (var p in disk.Partitions)
                        {
                            if (string.IsNullOrEmpty(p.DriveLetter)) continue;

                            double freePercentage = (double)p.FreeSpace / p.TotalSize * 100;
                            if (freePercentage < 8)
                            {
                                issuesBag.Add(new ScannerIssue
                                {
                                    Id = $"LOW_DISK_{p.DriveLetter}",
                                    Title = $"Critical Disk Space: {p.DriveLetter}",
                                    Description = $"Drive {p.DriveLetter} has less than {freePercentage:F1}% free space ({p.FreeSpace / 1024 / 1024 / 1024} GB). System performance may be severely impacted.",
                                    TechnicalDetails = $"Volume Label: {p.Label}\nFile System: {p.FileSystem}\nUsed Space: {(p.TotalSize - p.FreeSpace) / 1024 / 1024 / 1024} GB / {p.TotalSize / 1024 / 1024 / 1024} GB\nRaw Free Space Bytes: {p.FreeSpace}",
                                    Severity = freePercentage < 5 ? IssueSeverity.Required : IssueSeverity.Recommended,
                                    ActionType = ScannerActionType.ManualNavigation,
                                    ActionTarget = "Storage",
                                    Category = "Storage"
                                });
                            }
                        }
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 2: Security & Unsigned Binary Audit
            var securityTask = Task.Run(async () =>
            {
                try
                {
                    var defender = await _securityService.GetDefenderStatusAsync();
                    if (!defender.AntivirusEnabled || !defender.RealTimeProtection)
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "DEFENDER_DISABLED",
                            Title = "System Protection Disabled",
                            Description = "Windows Defender or Real-time protection is currently disabled. Your system is vulnerable to security threats.",
                            TechnicalDetails = $"Antivirus Status: {(defender.AntivirusEnabled ? "Active" : "Disabled")}\nReal-Time Guard Status: {(defender.RealTimeProtection ? "Running" : "Stopped")}\nWMI Security Provider: Microsoft Defender Security Service",
                            Severity = IssueSeverity.Required,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Security",
                            Category = "Security"
                        });
                    }

                    var unsigned = await _creatorService.GetUnsignedProcessesAsync();
                    if (unsigned.Any())
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "UNSIGNED_PROCESSES",
                            Title = "Unsigned Processes Detected",
                            Description = $"{unsigned.Count} processes are running without a valid digital signature. These could be malicious or unauthorized binaries.",
                            TechnicalDetails = $"Unsigned Executables:\n" + string.Join("\n", unsigned.Take(5).Select(ps => $"  - PID: {ps.PID} | Name: {ps.Name} | Path: {ps.Path}")) + (unsigned.Count > 5 ? $"\n  - ... and {unsigned.Count - 5} more processes." : ""),
                            Severity = IssueSeverity.Required,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Dashboard",
                            Category = "Security"
                        });
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 3: Firmware & BIOS Age Check
            var biosTask = Task.Run(async () =>
            {
                try
                {
                    var biosInfo = await _biosService.GetBiosInfoAsync();
                    if (DateTime.TryParse(biosInfo.ReleaseDate, out DateTime biosDate))
                    {
                        var age = DateTime.Now - biosDate;
                        if (age.TotalDays > 365 * 2) // > 2 years
                        {
                            issuesBag.Add(new ScannerIssue
                            {
                                Id = "OUTDATED_BIOS",
                                Title = "Outdated BIOS/Firmware",
                                Description = $"Your BIOS was last updated on {biosInfo.ReleaseDate} ({age.TotalDays / 365:F1} years ago). Outdated firmware can lead to stability and security issues.",
                                TechnicalDetails = $"BIOS Vendor: {biosInfo.Vendor}\nSMBIOS Version: {biosInfo.Version}\nRelease Date: {biosInfo.ReleaseDate}\nAge Threshold: 2.0 Years (Current: {age.TotalDays / 365:F2} Years)",
                                Severity = IssueSeverity.Recommended,
                                ActionType = ScannerActionType.ManualNavigation,
                                ActionTarget = "Bios",
                                Category = "System"
                            });
                        }
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 4: Performance & Memory Pressure Audit
            var perfTask = Task.Run(async () =>
            {
                try
                {
                    var perf = await _performanceService.GetCurrentSnapshotAsync();
                    if (perf.RamUsage > 88)
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "HIGH_RAM",
                            Title = "High Memory Pressure",
                            Description = $"Current RAM usage is at {perf.RamUsage:F1}%. High memory pressure can cause system instability and lag.",
                            TechnicalDetails = $"Memory Load: {perf.RamUsage:F1}%\nAvailable Standby Cache: Standby Working Set pages can be purged to reclaim immediate capacity.",
                            Severity = perf.RamUsage > 94 ? IssueSeverity.Required : IssueSeverity.Recommended,
                            ActionType = ScannerActionType.AutoFix,
                            ActionTarget = "PurgeRAM",
                            Category = "Performance"
                        });
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 5: Network Ping Latency Audit
            var networkTask = Task.Run(async () =>
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync("8.8.8.8", 1500);
                    if (reply.Status == IPStatus.Success)
                    {
                        if (reply.RoundtripTime > 150)
                        {
                            issuesBag.Add(new ScannerIssue
                            {
                                Id = "HIGH_LATENCY",
                                Title = "High Network Latency",
                                Description = $"Network latency is high ({reply.RoundtripTime}ms). This may impact online diagnostic synchronization and overall system connectivity.",
                                TechnicalDetails = $"Audit Target: 8.8.8.8 (Google DNS)\nPing Roundtrip: {reply.RoundtripTime} ms\nStatus Code: {reply.Status}\nThreshold Warning: 150 ms",
                                Severity = IssueSeverity.Recommended,
                                ActionType = ScannerActionType.ManualNavigation,
                                ActionTarget = "Network",
                                Category = "Network"
                            });
                        }
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 6: Reclaimable Temp & Minidump Capacity Audit
            var tempScanTask = Task.Run(() =>
            {
                try
                {
                    long totalBytes = 0;
                    string userTemp = Path.GetTempPath();
                    string systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                    string minidumpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump");

                    void CountFolder(string path)
                    {
                        if (!Directory.Exists(path)) return;
                        var dir = new DirectoryInfo(path);
                        foreach (var f in dir.GetFiles("*", SearchOption.AllDirectories))
                        {
                            try { totalBytes += f.Length; } catch { }
                        }
                    }

                    CountFolder(userTemp);
                    CountFolder(systemTemp);
                    CountFolder(minidumpPath);

                    long mbReclaimable = totalBytes / 1024 / 1024;
                    if (mbReclaimable > 150)
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "TEMP_FILES",
                            Title = $"Reclaimable Temp Cache: {mbReclaimable} MB",
                            Description = $"Over {mbReclaimable} MB of system temporary files, crash minidumps, and log caches have accumulated.",
                            TechnicalDetails = $"User Temp Path: {userTemp}\nSystem Temp Path: {systemTemp}\nReclaimable Storage: {mbReclaimable} MB ({totalBytes} bytes)",
                            Severity = mbReclaimable > 1024 ? IssueSeverity.Recommended : IssueSeverity.Optional,
                            ActionType = ScannerActionType.AutoFix,
                            ActionTarget = "ClearTemp",
                            Category = "Cleanup"
                        });
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 7: Windows Pending Reboot & Stale Update Audit
            var rebootAuditTask = Task.Run(() =>
            {
                try
                {
                    bool isPendingReboot = false;
                    string details = "";

                    using (var key1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
                    {
                        if (key1 != null) { isPendingReboot = true; details += "• Component Based Servicing pending reboot flag set\n"; }
                    }

                    using (var key2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                    {
                        if (key2 != null) { isPendingReboot = true; details += "• Windows Update pending restart flag set\n"; }
                    }

                    if (isPendingReboot)
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "PENDING_REBOOT",
                            Title = "System Restart Pending",
                            Description = "A Windows update or component installation requires a system restart to complete changes.",
                            TechnicalDetails = $"Pending Reboot Flags:\n{details.TrimEnd()}",
                            Severity = IssueSeverity.Recommended,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "System",
                            Category = "System"
                        });
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Task 8: Driver Integrity Audit
            var driverTask = Task.Run(async () =>
            {
                try
                {
                    var drivers = await _driversService.GetInstalledDriversAsync();
                    var criticalDevices = drivers.Where(d => 
                        (d.Name.Contains("Graphics", StringComparison.OrdinalIgnoreCase) || 
                         d.Name.Contains("Display", StringComparison.OrdinalIgnoreCase) ||
                         d.Name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
                         d.Name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                         d.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)) &&
                        d.Type == "System (Microsoft)"
                    ).ToList();

                    if (criticalDevices.Any())
                    {
                        issuesBag.Add(new ScannerIssue
                        {
                            Id = "GENERIC_DRIVERS",
                            Title = "Generic Drivers Detected",
                            Description = $"{criticalDevices.Count} critical devices (GPU/Network) are using generic Microsoft drivers. Official manufacturer drivers are recommended for full performance.",
                            TechnicalDetails = $"Identified Devices:\n" + string.Join("\n", criticalDevices.Take(4).Select(d => $"  - {d.Name} (Provider: {d.Provider})")) + (criticalDevices.Count > 4 ? $"\n  - ... and {criticalDevices.Count - 4} more devices." : ""),
                            Severity = IssueSeverity.Recommended,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Drivers",
                            Category = "Drivers"
                        });
                    }
                }
                catch { }
                finally { ReportProgress(); }
            });

            // Await all parallel diagnostic tasks simultaneously
            await Task.WhenAll(
                storageTask, securityTask, biosTask, perfTask,
                networkTask, tempScanTask, rebootAuditTask, driverTask
            );

            progress?.Report(100);
            return issuesBag.ToList();
        }

        public async Task<bool> ExecuteFixAsync(ScannerIssue issue)
        {
            switch (issue.ActionTarget)
            {
                case "PurgeRAM":
                    var ramResult = await _creatorService.PurgeStandbyMemoryAsync();
                    return ramResult.Success;
                case "ClearTemp":
                    var bytesCleared = await _toolkitService.ClearTempFilesAsync();
                    return bytesCleared >= 0;
                default:
                    return false;
            }
        }
    }
}

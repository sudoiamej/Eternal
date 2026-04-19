using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
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
            var issues = new List<ScannerIssue>();

            // 1. Storage Scan (15%)
            progress.Report(10);
            var partitions = await _storageService.GetPartitionsAsync();
            foreach (var p in partitions)
            {
                double freePercentage = (double)p.FreeSpace / p.TotalSize * 100;
                if (freePercentage < 5)
                {
                    issues.Add(new ScannerIssue
                    {
                        Id = $"LOW_DISK_{p.DriveLetter}",
                        Title = $"Critical Disk Space: {p.DriveLetter}",
                        Description = $"Drive {p.DriveLetter} has less than 5% free space ({p.FreeSpace / 1024 / 1024 / 1024} GB). System performance may be severely impacted.",
                        Severity = IssueSeverity.Required,
                        ActionType = ScannerActionType.ManualNavigation,
                        ActionTarget = "Storage",
                        Category = "Storage"
                    });
                }
            }

            // 2. Security Scan (30%)
            progress.Report(30);
            var defender = await _securityService.GetDefenderStatusAsync();
            if (!defender.AntivirusEnabled || !defender.RealTimeProtection)
            {
                issues.Add(new ScannerIssue
                {
                    Id = "DEFENDER_DISABLED",
                    Title = "System Protection Disabled",
                    Description = "Windows Defender or Real-time protection is currently disabled. Your system is vulnerable to security threats.",
                    Severity = IssueSeverity.Required,
                    ActionType = ScannerActionType.ManualNavigation,
                    ActionTarget = "Security",
                    Category = "Security"
                });
            }

            var unsigned = await _creatorService.GetUnsignedProcessesAsync();
            if (unsigned.Any())
            {
                issues.Add(new ScannerIssue
                {
                    Id = "UNSIGNED_PROCESSES",
                    Title = "Unsigned Processes Detected",
                    Description = $"{unsigned.Count} processes are running without a valid digital signature. These could be malicious or unauthorized binaries.",
                    Severity = IssueSeverity.Required,
                    ActionType = ScannerActionType.ManualNavigation,
                    ActionTarget = "Dashboard", // Dashboard has the Threat Hunter view
                    Category = "Security"
                });
            }

            // 3. BIOS Age Check (45%)
            progress.Report(45);
            try
            {
                var biosInfo = await _biosService.GetBiosInfoAsync();
                if (DateTime.TryParse(biosInfo.ReleaseDate, out DateTime biosDate))
                {
                    var age = DateTime.Now - biosDate;
                    if (age.TotalDays > 365 * 2) // > 2 years
                    {
                        issues.Add(new ScannerIssue
                        {
                            Id = "OUTDATED_BIOS",
                            Title = "Outdated BIOS/Firmware",
                            Description = $"Your BIOS was last updated on {biosInfo.ReleaseDate} ({age.TotalDays / 365:F1} years ago). Outdated firmware can lead to stability and security issues.",
                            Severity = IssueSeverity.Recommended,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Bios",
                            Category = "System"
                        });
                    }
                }
            }
            catch { }

            // 4. Performance & Network (60%)
            progress.Report(60);
            var perf = await _performanceService.GetCurrentSnapshotAsync();
            if (perf.RamUsage > 90)
            {
                issues.Add(new ScannerIssue
                {
                    Id = "HIGH_RAM",
                    Title = "High Memory Pressure",
                    Description = $"Current RAM usage is at {perf.RamUsage:F1}%. High memory pressure can cause system instability and lag.",
                    Severity = IssueSeverity.Recommended,
                    ActionType = ScannerActionType.AutoFix,
                    ActionTarget = "PurgeRAM",
                    Category = "Performance"
                });
            }

            // Network Latency Check
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                if (reply.Status == IPStatus.Success)
                {
                    if (reply.RoundtripTime > 150)
                    {
                        issues.Add(new ScannerIssue
                        {
                            Id = "HIGH_LATENCY",
                            Title = "High Network Latency",
                            Description = $"Network latency is high ({reply.RoundtripTime}ms). This may impact online diagnostic synchronization and overall system connectivity.",
                            Severity = IssueSeverity.Recommended,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Network",
                            Category = "Network"
                        });
                    }
                    else if (reply.RoundtripTime > 80)
                    {
                        issues.Add(new ScannerIssue
                        {
                            Id = "MODERATE_LATENCY",
                            Title = "Moderate Network Latency",
                            Description = $"Network latency is slightly elevated ({reply.RoundtripTime}ms). Connection performance is suboptimal.",
                            Severity = IssueSeverity.Optional,
                            ActionType = ScannerActionType.ManualNavigation,
                            ActionTarget = "Network",
                            Category = "Network"
                        });
                    }
                }
            }
            catch { }

            // 5. System Cleanup (85%)
            progress.Report(85);
            issues.Add(new ScannerIssue
            {
                Id = "TEMP_FILES",
                Title = "Temporary File Accumulation",
                Description = "System temporary files and caches have accumulated. Cleaning these can reclaim space and improve file system responsiveness.",
                Severity = IssueSeverity.Optional,
                ActionType = ScannerActionType.AutoFix,
                ActionTarget = "ClearTemp",
                Category = "Cleanup"
            });

            progress.Report(100);

            // 6. Driver Integrity Scan (95%)
            progress.Report(95);
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
                issues.Add(new ScannerIssue
                {
                    Id = "GENERIC_DRIVERS",
                    Title = "Generic Drivers Detected",
                    Description = $"{criticalDevices.Count} critical devices (GPU/Network) are using generic Microsoft drivers. Official manufacturer drivers are recommended for full performance and stability.",
                    Severity = IssueSeverity.Recommended,
                    ActionType = ScannerActionType.ManualNavigation,
                    ActionTarget = "Drivers",
                    Category = "Drivers"
                });
            }

            return issues;
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

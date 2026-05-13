using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;
using Eternal.Services.Security;
using Eternal.Helpers;

namespace Eternal.Services.System
{
    public class WindowsIntelligenceService : IIntelligenceService
    {
        private readonly IPerformanceService _performanceService;
        private readonly ISecurityService _securityService;

        public WindowsIntelligenceService(IPerformanceService performanceService, ISecurityService securityService)
        {
            _performanceService = performanceService;
            _securityService = securityService;
        }

        public async Task<List<Anomaly>> GetSystemAnomaliesAsync()
        {
            var anomalies = new List<Anomaly>();
            
            try
            {
                var snap = await _performanceService.GetCurrentSnapshotAsync();
                float cpu = snap.CpuUsage;
                float ramPercent = snap.RamUsage;

                if (ramPercent > 85)
                {
                    anomalies.Add(new Anomaly(
                        "Memory (RAM)", 
                        "High memory utilization detected.", 
                        SeverityLevel.High,
                        new HumanExplanation(
                            $"{ramPercent:F1}%", 
                            "Your system is under severe memory pressure. Background applications may be suspended and active applications may stutter.", 
                            SeverityLevel.High)
                    ));
                }
                else if (ramPercent > 70)
                {
                    anomalies.Add(new Anomaly(
                        "Memory (RAM)", 
                        "Moderate memory utilization detected.", 
                        SeverityLevel.Medium,
                        new HumanExplanation(
                            $"{ramPercent:F1}%", 
                            "Your system is using a lot of memory. It is performing okay, but opening large applications might cause slight delays.", 
                            SeverityLevel.Medium)
                    ));
                }

                if (cpu > 90)
                {
                    anomalies.Add(new Anomaly(
                        "Processor (CPU)",
                        "Sustained high CPU usage.",
                        SeverityLevel.Critical,
                        new HumanExplanation(
                            $"{cpu:F0}%",
                            "The processor is heavily loaded. The system might feel unresponsive and the cooling fans will spin up to maximum.",
                            SeverityLevel.Critical)
                    ));
                }
            }
            catch { }

            if (anomalies.Count == 0)
            {
                anomalies.Add(new Anomaly(
                    "System Overall",
                    "No significant anomalies detected.",
                    SeverityLevel.Info,
                    new HumanExplanation("Normal", "Your system is running smoothly with no detected issues.", SeverityLevel.Info)
                ));
            }

            return anomalies;
        }

        public async Task<TrustScore> CalculateTrustScoreAsync()
        {
            // 1. Defender Status (40 points)
            int systemFileScore = 100;
            try
            {
                // Real Secure Boot check via Registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("UEFISecureBootEnabled");
                        if (val != null && (int)val == 0) systemFileScore = 50; // Secure Boot Disabled
                    }
                    else systemFileScore = 0; // Registry key missing/Access denied (Insecure state)
                }
            }
            catch { systemFileScore = 75; } // Heuristic fallback

            var defender = await _securityService.GetDefenderStatusAsync();
            int defenderScore = 0;
            if (defender.AntivirusEnabled) defenderScore += 20;
            if (defender.RealTimeProtection) defenderScore += 20;

            // 2. Startup Programs (20 points)
            var startups = await _securityService.GetStartupProgramsAsync();
            int startupScore = 100;
            if (startups.Count > 15) startupScore = 70;
            if (startups.Count > 30) startupScore = 40;

            // 3. Driver Signatures (20 points)
            var drivers = await _securityService.GetDriverSignaturesAsync();
            int driverScore = 100;
            if (drivers.Count > 0)
            {
                int unsigned = drivers.Count(d => !d.IsSigned);
                double unsignedPercent = (double)unsigned / drivers.Count;
                driverScore = (int)(100 - (unsignedPercent * 100));
            }

            // 4. Network Score (20 points) - Adaptive Firewall Profile check
            int networkScore = 100;
            bool useNative = OsHelper.IsWindows11OrGreater();

            if (useNative)
            {
                try
                {
                    string[] profiles = { "StandardProfile", "PublicProfile", "DomainProfile" };
                    foreach (var profile in profiles)
                    {
                        var enabled = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{profile}", "EnableFirewall", 1);
                        if (enabled != null && (int)enabled == 0)
                        {
                            networkScore -= 33;
                        }
                    }
                }
                catch { networkScore = 90; }
            }
            
            if (networkScore == 100 || !useNative)
            {
                try
                {
                    // WMI Fallback/Legacy
                    var searcher = new ManagementObjectSearcher(@"root\StandardCimv2", "SELECT Enabled FROM MSFT_NetFirewallProfile");
                    foreach (var obj in searcher.Get())
                    {
                        if (obj["Enabled"] != null && (ushort)obj["Enabled"] != 1) // 1 = Enabled
                        {
                            networkScore -= 33; // Deduct for each disabled profile (Domain, Private, Public)
                        }
                    }
                }
                catch { if (!useNative) networkScore = 95; } 
            }

            // Weighted average
            int overall = (int)((defenderScore * 2.5 * 0.4) + (startupScore * 0.2) + (driverScore * 0.2) + (networkScore * 0.2));

            TrustLevel level = overall switch
            {
                > 85 => TrustLevel.Safe,
                > 60 => TrustLevel.Warning,
                _ => TrustLevel.Critical
            };

            string explanation = level switch
            {
                TrustLevel.Safe => "System integrity is high. Real-time protection is active and core components are verified.",
                TrustLevel.Warning => "Security posture is weakened. Some protection features may be disabled or unverified components found.",
                _ => "Critical security risks detected. Real-time protection may be off or significant unverified components are present."
            };

            return new TrustScore(overall, level, explanation, startupScore, driverScore, systemFileScore, networkScore);
        }

        public Task<List<RootCause>> GetPerformanceRootCausesAsync()
        {
            return Task.Run(() =>
            {
                var causes = new List<RootCause>();

                try
                {
                    var processes = Process.GetProcesses()
                        .OrderByDescending(p => p.WorkingSet64)
                        .Take(3)
                        .ToList();

                    foreach (var process in processes)
                    {
                        if (process.WorkingSet64 > 500 * 1024 * 1024)
                        {
                            double mb = process.WorkingSet64 / (1024.0 * 1024.0);
                            causes.Add(new RootCause(
                                "Memory",
                                process.ProcessName,
                                $"Process is consuming {mb:F0} MB of RAM.",
                                new HumanExplanation(
                                    $"{mb:F0} MB",
                                    $"'{process.ProcessName}' is using a significant amount of memory. If you are not actively using it, closing it will free up system resources.",
                                    SeverityLevel.Medium)
                            ));
                        }
                    }
                }
                catch { }

                return causes;
            });
        }
    }
}

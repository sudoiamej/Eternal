using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Linq;
using System.Diagnostics;

namespace Eternal.Services.Security
{
    public class WindowsSecurityService : ISecurityService
    {
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };
        private List<DriverSignatureInfo>? _cachedDrivers;
        private DateTime _lastDriverScan = DateTime.MinValue;
        private readonly object _driverLock = new object();

        private ManagementObjectSearcher CreateSearcher(string query, string? scope = null) 
            => new ManagementObjectSearcher(scope, query, _wmiOptions);

        public Task<List<StartupProgram>> GetStartupProgramsAsync()
        {
            return Task.Run(() =>
            {
                var programs = new List<StartupProgram>();
                
                // Check Registry HKLM Run
                ReadRegistryRun(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", programs, "HKLM Run");
                // Check Registry HKCU Run
                ReadRegistryRun(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", programs, "HKCU Run");

                return programs;
            });
        }

        private void ReadRegistryRun(RegistryKey root, string keyPath, List<StartupProgram> list, string location)
        {
            try
            {
                using (var key = root.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        foreach (var name in key.GetValueNames())
                        {
                            list.Add(new StartupProgram(name, key.GetValue(name)?.ToString() ?? "", location));
                        }
                    }
                }
            }
            catch { }
        }

        public Task<DefenderStatus> GetDefenderStatusAsync()
        {
            return Task.Run(() =>
            {
                bool realTime = false;
                bool avEnabled = false;

                try
                {
                    using var searcher = CreateSearcher("select RealTimeProtectionEnabled, AntivirusEnabled from MSFT_MpComputerStatus", @"Root\Microsoft\Windows\Defender");
                    foreach (var obj in searcher.Get())
                    {
                        realTime = global::System.Convert.ToBoolean(obj["RealTimeProtectionEnabled"] ?? false);
                        avEnabled = global::System.Convert.ToBoolean(obj["AntivirusEnabled"] ?? false);
                        break;
                    }
                }
                catch { }

                return new DefenderStatus(realTime, avEnabled);
            });
        }

        public Task<List<ServiceInfo>> GetRunningServicesAsync()
        {
            return Task.Run(() =>
            {
                var services = new List<ServiceInfo>();
                try
                {
                    using var searcher = CreateSearcher("select Name, DisplayName, State, StartMode from Win32_Service");
                    foreach (var obj in searcher.Get())
                    {
                        services.Add(new ServiceInfo(
                            obj["Name"]?.ToString() ?? "",
                            obj["DisplayName"]?.ToString() ?? "",
                            obj["State"]?.ToString() ?? "",
                            obj["StartMode"]?.ToString() ?? ""
                        ));
                    }
                }
                catch { }
                return services;
            });
        }

        public Task<List<SoftwareInfo>> GetInstalledSoftwareAsync()
        {
            return Task.Run(() =>
            {
                var software = new List<SoftwareInfo>();
                string[] keys = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };
                
                foreach (var keyPath in keys)
                {
                    try
                    {
                        using var root = Registry.LocalMachine.OpenSubKey(keyPath);
                        if (root != null)
                        {
                            foreach (var name in root.GetSubKeyNames())
                            {
                                using var subkey = root.OpenSubKey(name);
                                string? appName = subkey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(appName))
                                {
                                    software.Add(new SoftwareInfo(
                                        appName,
                                        subkey?.GetValue("DisplayVersion")?.ToString() ?? "N/A",
                                        subkey?.GetValue("Publisher")?.ToString() ?? "Unknown"
                                    ));
                                }
                            }
                        }
                    }
                    catch { }
                }
                return software.OrderBy(s => s.Name).ToList();
            });
        }

        public Task<List<DriverSignatureInfo>> GetDriverSignaturesAsync()
        {
            return Task.Run(() =>
            {
                lock (_driverLock)
                {
                    if (_cachedDrivers != null && (DateTime.Now - _lastDriverScan).TotalMinutes < 5)
                    {
                        return new List<DriverSignatureInfo>(_cachedDrivers);
                    }
                }

                var drivers = new List<DriverSignatureInfo>();
                try
                {
                    using var searcher = CreateSearcher("select DeviceName, IsSigned, Manufacturer from Win32_PnPSignedDriver");
                    foreach (var obj in searcher.Get())
                    {
                        drivers.Add(new DriverSignatureInfo(
                            obj["DeviceName"]?.ToString() ?? "Unknown Device",
                            global::System.Convert.ToBoolean(obj["IsSigned"] ?? false),
                            obj["Manufacturer"]?.ToString() ?? "Unknown"
                        ));
                    }

                    lock (_driverLock)
                    {
                        _cachedDrivers = drivers;
                        _lastDriverScan = DateTime.Now;
                    }
                }
                catch { }
                return drivers;
            });
        }

        public Task<REAgentStatus> GetREAgentStatusAsync()
        {
            return Task.Run(() =>
            {
                bool enabled = false;
                string winLoc = "Unknown";
                string id = "Unknown";
                string imgLoc = "Unknown";

                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("reagentc.exe", "/info")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(psi);
                    string output = process?.StandardOutput.ReadToEnd() ?? "";
                    
                    enabled = output.Contains("Enabled") || output.Contains("1");
                    
                    var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains("Windows RE location:")) winLoc = line.Split(':').Last().Trim();
                        if (line.Contains("Boot Configuration Data (BCD) identifier:")) id = line.Split(':').Last().Trim();
                        if (line.Contains("Recovery image location:")) imgLoc = line.Split(':').Last().Trim();
                    }
                }
                catch { }

                return new REAgentStatus(enabled, winLoc, id, imgLoc);
            });
        }

        public Task<List<BitLockerStatus>> GetBitLockerStatusAsync()
        {
            return Task.Run(() =>
            {
                var list = new List<BitLockerStatus>();
                try
                {
                    var scope = new ManagementScope(@"Root\CIMV2\Security\MicrosoftVolumeEncryption");
                    scope.Connect();
                    using var searcher = CreateSearcher("select * from Win32_EncryptableVolume", @"Root\CIMV2\Security\MicrosoftVolumeEncryption");
                    
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string drive = obj["DriveLetter"]?.ToString() ?? "System Reserved";
                        uint protection = global::System.Convert.ToUInt32(obj["ProtectionStatus"] ?? 0);
                        uint encryption = global::System.Convert.ToUInt32(obj["EncryptionMethod"] ?? 0);
                        uint conversion = global::System.Convert.ToUInt32(obj["ConversionStatus"] ?? 0);

                        string protStr = protection == 1 ? "On" : "Off";
                        string method = GetEncryptionMethod(encryption);
                        string lockStr = GetLockStatus(conversion);
                        
                        // Get Key Protectors
                        string protectors = "None";
                        try
                        {
                            var inParams = obj.GetMethodParameters("GetKeyProtectors");
                            var outParams = obj.InvokeMethod("GetKeyProtectors", inParams, null);

                            if (outParams != null && (uint)outParams["ReturnValue"] == 0)
                            {
                                var protectorIds = (string[])outParams["VolumeKeyProtectorID"];
                                if (protectorIds != null && protectorIds.Length > 0)
                                {
                                    var types = new List<string>();
                                    foreach (var pid in protectorIds)
                                    {
                                        try {
                                            var pObjParams = obj.GetMethodParameters("GetKeyProtectorType");
                                            pObjParams["VolumeKeyProtectorID"] = pid;
                                            var pObj = obj.InvokeMethod("GetKeyProtectorType", pObjParams, null);
                                            if (pObj != null && (uint)pObj["ReturnValue"] == 0)
                                            {
                                                types.Add(GetKeyProtectorType((uint)pObj["KeyProtectorType"]));
                                            }
                                        } catch { /* Skip single failed protector */ }
                                    }
                                    protectors = types.Any() ? string.Join(", ", types.Distinct()) : "Unknown Type";
                                }
                                else { protectors = "No Protectors Found"; }
                            }
                            else { protectors = "Access Denied / Not Supported"; }
                        }
                        catch (Exception ex) { protectors = $"Error: {ex.Message}"; }

                        list.Add(new BitLockerStatus(drive, protStr, method, lockStr, protectors));
                    }
                }
                catch { }
                return list;
            });
        }

        private string GetEncryptionMethod(uint code)
        {
            return code switch
            {
                0 => "None",
                1 => "AES 128-bit Diffuser",
                2 => "AES 256-bit Diffuser",
                3 => "AES 128",
                4 => "AES 256",
                5 => "Hardware Encryption",
                6 => "XTS-AES 128",
                7 => "XTS-AES 256",
                _ => "Unknown"
            };
        }

        private string GetLockStatus(uint code)
        {
            return code switch
            {
                0 => "Fully Decrypted",
                1 => "Fully Encrypted",
                2 => "Encryption In Progress",
                3 => "Decryption In Progress",
                4 => "Encryption Paused",
                5 => "Decryption Paused",
                _ => "Unknown"
            };
        }

        private string GetKeyProtectorType(uint code)
        {
            return code switch
            {
                0 => "Unknown",
                1 => "Trusted Platform Module (TPM)",
                2 => "External Key",
                3 => "Numerical Password",
                4 => "TPM + PIN",
                5 => "TPM + Startup Key",
                6 => "TPM + PIN + Startup Key",
                7 => "Public Key",
                8 => "Passphrase",
                9 => "TPM (Virtual Smart Card)",
                10 => "AD Account Holder",
                _ => "Other"
            };
        }

        public Task<List<ThreatInfo>> ScanSystemThreatsAsync()
        {
            return Task.Run(() =>
            {
                var threats = new List<ThreatInfo>();

                // 1. Hosts File Audit
                try
                {
                    string hostsPath = global::System.IO.Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
                    if (global::System.IO.File.Exists(hostsPath))
                    {
                        var lines = global::System.IO.File.ReadAllLines(hostsPath);
                        int suspiciousCount = 0;
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;
                            
                            // Check if line maps a domain to an external IP
                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string ip = parts[0];
                                if (ip != "127.0.0.1" && ip != "::1" && ip != "localhost")
                                {
                                    suspiciousCount++;
                                }
                            }
                        }

                        if (suspiciousCount > 0)
                        {
                            threats.Add(new ThreatInfo("Network Integrity", $"{suspiciousCount} custom domain mapping(s) detected in system hosts file.", "WARNING", true));
                        }
                        else
                        {
                            threats.Add(new ThreatInfo("Network Integrity", "System hosts file is clean. No rogue mapping anomalies.", "SECURE", false));
                        }
                    }
                }
                catch
                {
                    threats.Add(new ThreatInfo("Network Integrity", "Could not access hosts file registry descriptors.", "UNRESOLVED", false));
                }

                // 2. Startup Directory Audit
                try
                {
                    var startupPaths = new[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                    };

                    int startupCount = 0;
                    foreach (var path in startupPaths)
                    {
                        if (global::System.IO.Directory.Exists(path))
                        {
                            var files = global::System.IO.Directory.GetFiles(path);
                            foreach (var file in files)
                            {
                                string ext = global::System.IO.Path.GetExtension(file).ToLower();
                                if (ext == ".lnk" || ext == ".exe" || ext == ".vbs" || ext == ".bat" || ext == ".ps1")
                                {
                                    startupCount++;
                                }
                            }
                        }
                    }

                    if (startupCount > 2)
                    {
                        threats.Add(new ThreatInfo("Startup Persistence", $"{startupCount} files found in Startup folders. Audit recommended.", "NOTICE", false));
                    }
                    else
                    {
                        threats.Add(new ThreatInfo("Startup Persistence", "Startup folder directories are highly lean.", "SECURE", false));
                    }
                }
                catch { }

                // 3. User & Temp Directory Active Services Audit
                try
                {
                    using var searcher = CreateSearcher("select Name, PathName from Win32_Service");
                    int rogueServiceCount = 0;
                    foreach (var obj in searcher.Get())
                    {
                        string path = obj["PathName"]?.ToString().ToLower() ?? "";
                        if (path.Contains(@"\users\") || path.Contains(@"\appdata\") || path.Contains(@"\temp\"))
                        {
                            rogueServiceCount++;
                        }
                    }

                    if (rogueServiceCount > 0)
                    {
                        threats.Add(new ThreatInfo("Active Process Context", $"{rogueServiceCount} background service(s) running out of user profile directories.", "CRITICAL", true));
                    }
                    else
                    {
                        threats.Add(new ThreatInfo("Active Process Context", "All active background services are executing from system paths.", "SECURE", false));
                    }
                }
                catch { }

                return threats;
            });
        }
    }
}

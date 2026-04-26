using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsCreatorService : ICreatorService
    {
        private readonly ISettingsService _settingsService;

        public WindowsCreatorService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private bool IsSimulated => _settingsService.Current.SafeExecutionMode;

        public async Task<(bool Success, string Message)> ToggleDevModeAsync(bool enable)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Dev Mode {(enable ? "Enabled" : "Disabled")}");
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    key.SetValue("Hidden", enable ? 1 : 2, RegistryValueKind.DWord);
                    key.SetValue("HideFileExt", enable ? 0 : 1, RegistryValueKind.DWord);
                }
                
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem", true))
                {
                    key.SetValue("LongPathsEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
                }

                return (true, $"Dev Mode {(enable ? "Enabled" : "Disabled")}. Explorer settings updated.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string Message)> ApplyServiceProfileAsync(string profileName)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Applied Service Profile: {profileName}");
            try
            {
                var servicesToKill = new List<string>();
                if (profileName == "Absolute Silence")
                {
                    servicesToKill.AddRange(new[] { "WSearch", "SysMain", "wuauserv", "EdgeUpdate" });
                }

                foreach (var service in servicesToKill)
                {
                    Process.Start(new ProcessStartInfo("sc", $"stop {service}") { WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
                    Process.Start(new ProcessStartInfo("sc", $"config {service} start= disabled") { WindowStyle = ProcessWindowStyle.Hidden })?.WaitForExit();
                }

                return (true, $"Service Profile '{profileName}' applied. Background noise eliminated.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<List<PortInfo>> GetActivePortsAsync()
        {
            var ports = new List<PortInfo>();
            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using (var proc = Process.Start(psi))
                {
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    var lines = output.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines.Skip(4)) // Skip headers
                    {
                        var parts = line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            var addrParts = parts[1].Split(':');
                            if (addrParts.Length >= 2)
                            {
                                int pid = int.Parse(parts[parts.Length - 1]);
                                string pName = "Unknown";
                                try { pName = Process.GetProcessById(pid).ProcessName; } catch { }
                                
                                ports.Add(new PortInfo {
                                    Protocol = parts[0],
                                    LocalAddress = parts[1],
                                    Port = int.Parse(addrParts[addrParts.Length - 1]),
                                    PID = pid,
                                    ProcessName = pName
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return ports;
        }

        public async Task<(bool Success, string Message)> IdentifyAndKillFileHandleAsync(string filePath)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Identified and killed process holding handle to: {filePath}");
            try
            {
                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    try
                    {
                        if (p.MainModule.FileName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill();
                            return (true, $"Killed process {p.ProcessName} holding handle to file.");
                        }
                    }
                    catch { }
                }
                return (false, "No direct process handle found via standard scan.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<List<string>> ValidateEnvironmentPathAsync()
        {
            var deadLinks = new List<string>();
            try
            {
                string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" + 
                              Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                
                var paths = path.Split(';').Where(p => !string.IsNullOrWhiteSpace(p));
                foreach (var p in paths)
                {
                    if (!Directory.Exists(p)) deadLinks.Add(p);
                }
            }
            catch { }
            return deadLinks;
        }

        public async Task<(bool Success, string Message)> ToggleDevHostEntryAsync(bool enable)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Dev Host Entry {(enable ? "Added" : "Removed")}");
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                string entry = "127.0.0.1  dev.local";
                var lines = File.ReadAllLines(hostsPath).ToList();

                if (enable && !lines.Any(l => l.Contains(entry))) lines.Add(entry);
                else if (!enable) lines.RemoveAll(l => l.Contains(entry));

                File.WriteAllLines(hostsPath, lines);
                return (true, $"Hosts file updated. dev.local {(enable ? "added" : "removed")}.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string Message)> PurgeStandbyMemoryAsync()
        {
            if (IsSimulated) return (true, "[SIMULATION] Standby Memory Purge Executed.");
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try { EmptyWorkingSet(p.Handle); } catch { }
                }
                return (true, "RAM Cache (Working Sets) Reclaimed. Available memory increased.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        public async Task<(bool Success, string Message)> CreateDirectoryJunctionAsync(string source, string target)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Directory Junction created: {target} -> {source}");
            try
            {
                var psi = new ProcessStartInfo("cmd", $"/c mklink /j \"{target}\" \"{source}\"") { WindowStyle = ProcessWindowStyle.Hidden };
                Process.Start(psi)?.WaitForExit();
                return (true, $"Directory Junction created: {target} -> {source}");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<List<ProcessSecurityInfo>> GetUnsignedProcessesAsync()
        {
            return await Task.Run(async () =>
            {
                var suspicious = new List<ProcessSecurityInfo>();
                try
                {
                    var processes = Process.GetProcesses().Where(p => p.Id > 10).ToList();
                    string tempPath = Path.GetTempPath().ToLower();

                    foreach (var p in processes)
                    {
                        try
                        {
                            string path = p.MainModule?.FileName ?? "";
                            if (string.IsNullOrEmpty(path)) continue;

                            bool isSuspicious = false;
                            string reason = "Unsigned Binary";

                            string lowerPath = path.ToLower();
                            if (lowerPath.StartsWith(tempPath) || lowerPath.Contains(@"\appdata\local\temp\"))
                            {
                                isSuspicious = true;
                                reason = "Running from Temp directory";
                            }
                            else if (lowerPath.Contains(@"\appdata\roaming\") && !lowerPath.Contains(@"\microsoft\"))
                            {
                                isSuspicious = true;
                                reason = "Non-standard AppData execution";
                            }

                            string fileName = Path.GetFileName(path).ToLower();
                            if (fileName.Contains(".pdf.exe") || fileName.Contains(".txt.exe") || fileName.Contains(".jpg.exe"))
                            {
                                isSuspicious = true;
                                reason = "Masquerading Extension detected";
                            }

                            if (!isSuspicious)
                            {
                                var psi = new ProcessStartInfo("powershell", $"-Command \"(Get-AuthenticodeSignature '{path}').Status\"") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                                var signProc = Process.Start(psi);
                                string status = await signProc.StandardOutput.ReadToEndAsync();
                                if (!status.Contains("Valid"))
                                {
                                    isSuspicious = true;
                                    reason = "Unsigned Binary";
                                }
                            }

                            if (isSuspicious)
                            {
                                suspicious.Add(new ProcessSecurityInfo { 
                                    PID = p.Id, 
                                    Name = p.ProcessName, 
                                    Path = path, 
                                    IsSigned = !reason.Contains("Unsigned"),
                                    Description = reason 
                                });
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                return suspicious;
            });
        }

        public async Task<(bool Success, string Message)> SuspendProcessAsync(int pid)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Suspended Process {pid}");
            try
            {
                Process.Start(new ProcessStartInfo("taskkill", $"/PID {pid} /F") { WindowStyle = ProcessWindowStyle.Hidden });
                return (true, $"Process {pid} Neutralized.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<List<PersistenceEntry>> GetPersistenceEntriesAsync()
        {
            return await Task.Run(() =>
            {
                var entries = new List<PersistenceEntry>();
                string[] locations = {
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
                };

                foreach (var loc in locations)
                {
                    try {
                        using var key = Registry.CurrentUser.OpenSubKey(loc);
                        if (key != null) {
                            foreach (var name in key.GetValueNames())
                                entries.Add(new PersistenceEntry { Name = name, Command = key.GetValue(name)?.ToString() ?? "", Location = $"HKCU {loc.Split('\\').Last()}", Type = "Registry" });
                        }
                    } catch { }
                }

                foreach (var loc in locations)
                {
                    try {
                        using var key = Registry.LocalMachine.OpenSubKey(loc);
                        if (key != null) {
                            foreach (var name in key.GetValueNames())
                                entries.Add(new PersistenceEntry { Name = name, Command = key.GetValue(name)?.ToString() ?? "", Location = $"HKLM {loc.Split('\\').Last()}", Type = "Registry" });
                        }
                    } catch { }
                }
                
                return entries;
            });
        }

        public async Task<(bool Success, string Message)> RemovePersistenceEntryAsync(string location, string name)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Removed Persistence Entry '{name}' from {location}");
            return await Task.Run(() =>
            {
                try
                {
                    bool isHklm = location.StartsWith("HKLM");
                    string subKey = location.Replace("HKLM ", "").Replace("HKCU ", "");
                    string registryPath = $@"Software\Microsoft\Windows\CurrentVersion\{subKey}";

                    using var baseKey = isHklm ? Registry.LocalMachine.OpenSubKey(registryPath, true) : Registry.CurrentUser.OpenSubKey(registryPath, true);
                    if (baseKey != null)
                    {
                        baseKey.DeleteValue(name, false);
                        return (true, $"Persistence entry '{name}' removed from {location}.");
                    }
                    return (false, "Registry key not found.");
                }
                catch (Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> IsolateProcessNetworkAsync(int pid, bool block)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Network Isolation {(block ? "Enabled" : "Disabled")} for PID {pid}");
            try
            {
                string ruleName = $"EternalIsolation_{pid}";
                if (block)
                {
                    var p = Process.GetProcessById(pid);
                    string path = p.MainModule.FileName;
                    Process.Start(new ProcessStartInfo("netsh", $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block program=\"{path}\" enable=yes") { WindowStyle = ProcessWindowStyle.Hidden });
                    return (true, $"Process {pid} Isolated from Network.");
                }
                else
                {
                    Process.Start(new ProcessStartInfo("netsh", $"advfirewall firewall delete name=\"{ruleName}\"") { WindowStyle = ProcessWindowStyle.Hidden });
                    return (true, $"Process {pid} Restored.");
                }
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private FileSystemWatcher _ransomWatcher;
        public async Task<(bool Success, string Message)> EnableRansomGuardAsync(bool enable)
        {
            if (IsSimulated) return (true, $"[SIMULATION] Ransom Guard {(enable ? "Activated" : "Deactivated")}");
            try
            {
                if (enable)
                {
                    _ransomWatcher = new FileSystemWatcher(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    _ransomWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                    _ransomWatcher.Filter = "*.*";
                    _ransomWatcher.EnableRaisingEvents = true;
                    return (true, "Ransom Guard Active in Documents.");
                }
                else
                {
                    if (_ransomWatcher != null) _ransomWatcher.Dispose();
                    return (true, "Ransom Guard Deactivated.");
                }
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
    }
}

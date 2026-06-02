using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsRegistryService : IRegistryService
    {
        private static readonly Dictionary<string, string> KeyDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Programs that start automatically for all users on login." },
            { @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "Programs that run only once on the next login." },
            { @"Control Panel\Desktop", "Settings related to the appearance and behavior of the Windows desktop environment." },
            { @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "Policies controlling how Windows indexing and search features function." },
            { @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "Low-level kernel settings for how Windows handles physical and virtual memory." }
        };

        public Task<RegistryKeyInfo?> GetKeyAsync(string hive, string path)
        {
            try
            {
                using var baseKey = GetBaseKey(hive);
                using var key = baseKey.OpenSubKey(path);
                if (key == null) return Task.FromResult<RegistryKeyInfo?>(null);

                var info = new RegistryKeyInfo
                {
                    Name = key.Name.Split('\\').Last(),
                    FullPath = key.Name,
                    Hive = ParseHive(hive),
                    SubKeys = key.GetSubKeyNames().ToList(),
                    Values = key.GetValueNames().Select(v => new RegistryValueInfo
                    {
                        Name = v,
                        Value = key.GetValue(v) ?? string.Empty,
                        Kind = key.GetValueKind(v),
                        Description = GetValueDescription(path, v)
                    }).ToList()
                };

                return Task.FromResult<RegistryKeyInfo?>(info);
            }
            catch
            {
                return Task.FromResult<RegistryKeyInfo?>(null);
            }
        }

        public Task<List<RegistryValueInfo>> GetValuesAsync(string hive, string path)
        {
            try
            {
                using var baseKey = GetBaseKey(hive);
                using var key = baseKey.OpenSubKey(path);
                if (key == null) return Task.FromResult(new List<RegistryValueInfo>());

                var values = key.GetValueNames().Select(v => new RegistryValueInfo
                {
                    Name = v,
                    Value = key.GetValue(v) ?? string.Empty,
                    Kind = key.GetValueKind(v),
                    Description = GetValueDescription(path, v)
                }).ToList();

                return Task.FromResult(values);
            }
            catch
            {
                return Task.FromResult(new List<RegistryValueInfo>());
            }
        }

        public Task<bool> SetValueAsync(string hive, string path, string valueName, object value, RegistryValueKind kind)
        {
            try
            {
                using var baseKey = GetBaseKey(hive);
                using var key = baseKey.OpenSubKey(path, true);
                if (key == null) return Task.FromResult(false);

                if (value == null)
                {
                    key.DeleteValue(valueName, false);
                }
                else
                {
                    key.SetValue(valueName, value, kind);
                }
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private readonly bool _isWin11;
        private readonly bool _isWin10;

        public WindowsRegistryService()
        {
            _isWin11 = Eternal.Helpers.OsHelper.IsWindows11OrGreater();
            _isWin10 = Eternal.Helpers.OsHelper.IsWindows10OrGreater() && !_isWin11;
        }

        public Task<List<RegistryTweakDefinition>> GetCommonTweaksAsync()
        {
            var tweaks = new List<RegistryTweakDefinition>
            {
                new RegistryTweakDefinition
                {
                    Name = "UI Response Speed",
                    Description = "Reduces the delay before menus fly out when you hover over them.",
                    Hive = "HKCU",
                    KeyPath = @"Control Panel\Desktop",
                    ValueName = "MenuShowDelay",
                    Kind = RegistryValueKind.String,
                    IsWin10Compatible = true,
                    IsWin11Compatible = true,
                    Options = new List<RegistryOption>
                    {
                        new RegistryOption { Label = "Instant", Value = "0", Impact = "Snappier Feel" },
                        new RegistryOption { Label = "Default", Value = "400", Impact = "Standard Windows" }
                    }
                },
                new RegistryTweakDefinition
                {
                    Name = "Verbose Boot/Shutdown",
                    Description = "Shows detailed technical status messages during startup and shutdown instead of generic 'Please wait'.",
                    Hive = "HKLM",
                    KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    ValueName = "VerboseStatus",
                    Kind = RegistryValueKind.DWord,
                    IsWin10Compatible = true,
                    IsWin11Compatible = true,
                    Options = new List<RegistryOption>
                    {
                        new RegistryOption { Label = "Enabled (Expert)", Value = 1, Impact = "Better Troubleshooting" },
                        new RegistryOption { Label = "Disabled", Value = 0, Impact = "Cleaner UI" }
                    }
                },
                new RegistryTweakDefinition
                {
                    Name = "Compact OS Context Menu",
                    Description = "Restores the Windows 10 style context menu (no 'Show more options' click required).",
                    Hive = "HKCU",
                    KeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                    ValueName = "", // Default value
                    Kind = RegistryValueKind.String,
                    IsWin10Compatible = false,
                    IsWin11Compatible = true,
                    Options = new List<RegistryOption>
                    {
                        new RegistryOption { Label = "Windows 10 Style", Value = "", Impact = "Efficiency Enhanced" },
                        new RegistryOption { Label = "Windows 11 Style", Value = null!, Impact = "Reset to Default" }
                    }
                },
                new RegistryTweakDefinition
                {
                    Name = "Transparency Effects",
                    Description = "Disables acrylic/transparency effects in the Taskbar and Start Menu to save GPU resources.",
                    Hive = "HKCU",
                    KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    ValueName = "EnableTransparency",
                    Kind = RegistryValueKind.DWord,
                    IsWin10Compatible = true,
                    IsWin11Compatible = true,
                    Options = new List<RegistryOption>
                    {
                        new RegistryOption { Label = "Off (Faster)", Value = 0, Impact = "Higher FPS / Low Latency" },
                        new RegistryOption { Label = "On", Value = 1, Impact = "Visual Aesthetics" }
                    }
                },
                new RegistryTweakDefinition
                {
                    Name = "Game DVR / Game Bar",
                    Description = "Disables the background game recording features to improve gaming performance.",
                    Hive = "HKCU",
                    KeyPath = @"System\GameConfigStore",
                    ValueName = "GameDVR_Enabled",
                    Kind = RegistryValueKind.DWord,
                    IsWin10Compatible = true,
                    IsWin11Compatible = true,
                    Options = new List<RegistryOption>
                    {
                        new RegistryOption { Label = "Off (Faster)", Value = 0, Impact = "Less CPU Background Usage" },
                        new RegistryOption { Label = "On (Recorder Active)", Value = 1, Impact = "Standard Behavior" }
                    }
                }
            };

            foreach (var tweak in tweaks)
            {
                tweak.IsCurrentOSCompatible = EvaluateCompatibility(tweak);
                foreach (var option in tweak.Options)
                {
                    option.Parent = tweak;
                }
            }

            return Task.FromResult(tweaks);
        }

        private bool EvaluateCompatibility(RegistryTweakDefinition tweak)
        {
            if (_isWin11) return tweak.IsWin11Compatible;
            if (_isWin10) return tweak.IsWin10Compatible;
            return true;
        }

        public Task<RegistryValueKind> GetValueKindAsync(string hive, string path, string valueName)
        {
            try
            {
                using var baseKey = GetBaseKey(hive);
                using var key = baseKey.OpenSubKey(path);
                return Task.FromResult(key?.GetValueKind(valueName) ?? RegistryValueKind.Unknown);
            }
            catch { return Task.FromResult(RegistryValueKind.Unknown); }
        }

        public Task<RegistryProvenance> GetProvenanceAsync(string hive, string path)
        {
            var provenance = new RegistryProvenance();
            
            try
            {
                using var baseKey = GetBaseKey(hive);
                using var key = baseKey.OpenSubKey(path);
                if (key == null) return Task.FromResult(provenance);

                // Attempt to find ownership via common binary markers
                string? binaryPath = key.GetValue("ImagePath")?.ToString() 
                                  ?? key.OpenSubKey("InprocServer32")?.GetValue("")?.ToString();

                if (!string.IsNullOrEmpty(binaryPath))
                {
                    provenance.BinaryPath = binaryPath.Replace("\"", "").Trim();
                    var fileInfo = new FileInfo(provenance.BinaryPath);
                    if (fileInfo.Exists)
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(provenance.BinaryPath);
                        provenance.OwnerName = versionInfo.ProductName ?? fileInfo.Name;
                        provenance.Publisher = versionInfo.CompanyName ?? "Unknown Publisher";
                        provenance.IsSystemComponent = provenance.BinaryPath.Contains("Windows", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch { /* Ignore access errors for provenance */ }

            return Task.FromResult(provenance);
        }

        public async Task<List<RegistryWatchEntry>> CheckWatchlistDriftAsync(List<RegistryWatchEntry> watchlist)
        {
            foreach (var entry in watchlist)
            {
                try
                {
                    using var baseKey = GetBaseKey(entry.Hive);
                    using var key = baseKey.OpenSubKey(entry.KeyPath);
                    entry.CurrentValue = key?.GetValue(entry.ValueName) ?? "MISSING";
                    entry.IsDrifting = !entry.CurrentValue.Equals(entry.BaselineValue);
                }
                catch { entry.CurrentValue = "ACCESS_DENIED"; }
            }
            return watchlist;
        }

        public Task<string> GetKeyDescriptionAsync(string path)
        {
            if (KeyDescriptions.TryGetValue(path, out var desc)) return Task.FromResult(desc);
            return Task.FromResult("Generic Windows Registry path.");
        }

        private RegistryKey GetBaseKey(string hive)
        {
            return hive.ToUpper() switch
            {
                "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
                "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
                "HKEY_USERS" or "HKU" => Registry.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
                _ => throw new ArgumentException("Invalid registry hive.")
            };
        }

        private RegistryHiveType ParseHive(string hive)
        {
            return hive.ToUpper() switch
            {
                "HKEY_CLASSES_ROOT" or "HKCR" => RegistryHiveType.ClassesRoot,
                "HKEY_CURRENT_USER" or "HKCU" => RegistryHiveType.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => RegistryHiveType.LocalMachine,
                "HKEY_USERS" or "HKU" => RegistryHiveType.Users,
                "HKEY_CURRENT_CONFIG" or "HKCC" => RegistryHiveType.CurrentConfig,
                _ => RegistryHiveType.LocalMachine
            };
        }

        private string GetValueDescription(string keyPath, string valueName)
        {
            // Placeholder for value-specific intelligence
            return "No specific value intelligence found.";
        }

        public async Task<bool> MountOfflineHiveAsync(string hivePath, string mountName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(hivePath)) return false;

                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"load HKLM\\{mountName} \"{hivePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return false;
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error mounting registry hive: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> UnmountOfflineHiveAsync(string mountName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Reg unload requires admin privilege and that no handles are open.
                    // We run GC to release any registry key handles in our app process.
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = $"unload HKLM\\{mountName}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return false;
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error unmounting registry hive: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> RunDriverTriageMacroAsync(string mountName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var baseKey = Registry.LocalMachine;
                    using var mountedKey = baseKey.OpenSubKey(mountName, true);
                    if (mountedKey == null) return false;

                    string[] controlSets = { "ControlSet001", "ControlSet002" };
                    string[] drivers = { "stornvme", "storahci", "iastorv", "intelide", "pciide" };

                    bool modified = false;
                    foreach (var cs in controlSets)
                    {
                        using var csKey = mountedKey.OpenSubKey(cs, true);
                        if (csKey == null) continue;

                        using var servicesKey = csKey.OpenSubKey("Services", true);
                        if (servicesKey == null) continue;

                        foreach (var driver in drivers)
                        {
                            using var driverKey = servicesKey.OpenSubKey(driver, true);
                            if (driverKey != null)
                            {
                                driverKey.SetValue("Start", 0, RegistryValueKind.DWord);
                                modified = true;
                            }
                        }
                    }
                    return modified;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error running driver triage macro: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Management;
using Microsoft.Win32;
using Eternal.Models;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Eternal.Services.System
{
    public class WindowsOsUpdateService : IOsUpdateService
    {
        private const string UpdateRegistryPath = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        private readonly ISettingsService _settingsService;

        public WindowsOsUpdateService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private T RunInSTAThread<T>(Func<T> action)
        {
            T result = default!;
            Exception? exception = null;
            var thread = new global::System.Threading.Thread(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(global::System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                throw exception;
            }
            return result;
        }

        public async Task<List<WindowsUpdateItem>> GetAvailableUpdatesAsync()
        {
            return await Task.Run<List<WindowsUpdateItem>>(() =>
            {
                try
                {
                    return RunInSTAThread(() =>
                    {
                        var updates = new List<WindowsUpdateItem>();
                        Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                        if (sessionType == null) return updates;

                        dynamic session = Activator.CreateInstance(sessionType)!;
                        dynamic searcher = session.CreateUpdateSearcher();
                        
                        dynamic searchResult = searcher.Search("IsInstalled=0 and IsHidden=0");
                        dynamic updateCollection = searchResult.Updates;

                        for (int i = 0; i < updateCollection.Count; i++)
                        {
                            dynamic update = updateCollection.Item(i);
                            updates.Add(MapUpdate(update, false));
                        }
                        return updates;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching for updates: {ex.Message}");
                    return new List<WindowsUpdateItem>();
                }
            });
        }

        public async Task<List<WindowsUpdateItem>> GetInstalledUpdatesAsync()
        {
            return await Task.Run<List<WindowsUpdateItem>>(() =>
            {
                try
                {
                    return RunInSTAThread(() =>
                    {
                        var updates = new List<WindowsUpdateItem>();
                        Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                        if (sessionType == null) throw new Exception("WUA Session not available");

                        dynamic session = Activator.CreateInstance(sessionType)!;
                        dynamic searcher = session.CreateUpdateSearcher();
                        
                        dynamic searchResult = searcher.Search("IsInstalled=1");
                        dynamic updateCollection = searchResult.Updates;

                        int count = Math.Min(updateCollection.Count, 50);
                        for (int i = updateCollection.Count - 1; i >= updateCollection.Count - count; i--)
                        {
                            dynamic update = updateCollection.Item(i);
                            updates.Add(MapUpdate(update, true));
                        }
                        return updates;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching for installed updates: {ex.Message}. Using fallback...");
                    return GetInstalledUpdatesFallback();
                }
            });
        }

        private List<WindowsUpdateItem> GetInstalledUpdatesFallback()
        {
            var fallbackList = new List<WindowsUpdateItem>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
                using var results = searcher.Get();
                foreach (var obj in results)
                {
                    string kb = obj["HotFixID"]?.ToString() ?? "N/A";
                    string desc = obj["Description"]?.ToString() ?? "";
                    fallbackList.Add(new WindowsUpdateItem
                    {
                        Title = $"Security Update ({kb})",
                        KBArticle = kb,
                        Description = desc,
                        IsInstalled = true,
                        Status = WindowsUpdateStatus.Installed,
                        UpdateID = kb
                    });
                }
            }
            catch (Exception wmiEx)
            {
                Debug.WriteLine($"WMI Fallback failed: {wmiEx.Message}");
            }

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages");
                if (key != null)
                {
                    var names = key.GetSubKeyNames();
                    foreach (var name in names)
                    {
                        if (name.Contains("KB") && (name.Contains("Update") || name.Contains("Package")))
                        {
                            var match = Regex.Match(name, @"KB\d+");
                            if (match.Success)
                            {
                                string kb = match.Value;
                                if (!fallbackList.Any(u => u.KBArticle == kb))
                                {
                                    fallbackList.Add(new WindowsUpdateItem
                                    {
                                        Title = $"Windows Package ({kb})",
                                        KBArticle = kb,
                                        Description = "Installed system package",
                                        IsInstalled = true,
                                        Status = WindowsUpdateStatus.Installed,
                                        UpdateID = kb
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception regEx)
            {
                Debug.WriteLine($"Registry Fallback failed: {regEx.Message}");
            }

            return fallbackList.OrderByDescending(x => x.KBArticle).Take(50).ToList();
        }

        public async Task<bool> PauseUpdatesAsync(int days)
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.CreateSubKey(UpdateRegistryPath);
                    if (key == null) return false;

                    string startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string endTime = DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-ddTHH:mm:ssZ");

                    key.SetValue("PauseFeatureUpdatesStartTime", startTime, RegistryValueKind.String);
                    key.SetValue("PauseFeatureUpdatesEndTime", endTime, RegistryValueKind.String);
                    key.SetValue("PauseQualityUpdatesStartTime", startTime, RegistryValueKind.String);
                    key.SetValue("PauseQualityUpdatesEndTime", endTime, RegistryValueKind.String);
                    key.SetValue("PauseUpdatesStartTime", startTime, RegistryValueKind.String);
                    key.SetValue("PauseUpdatesExpiryTime", endTime, RegistryValueKind.String);

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ResumeUpdatesAsync()
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(UpdateRegistryPath, true);
                    if (key == null) return true;

                    string[] valuesToDelete = { 
                        "PauseFeatureUpdatesStartTime", "PauseFeatureUpdatesEndTime",
                        "PauseQualityUpdatesStartTime", "PauseQualityUpdatesEndTime",
                        "PauseUpdatesStartTime", "PauseUpdatesExpiryTime"
                    };

                    foreach (var val in valuesToDelete)
                    {
                        try { key.DeleteValue(val, false); } catch { }
                    }

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<(bool IsPaused, DateTime? ResumeDate)> GetPauseStatusAsync()
        {
            return await Task.Run<(bool, DateTime?)>(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(UpdateRegistryPath);
                    if (key == null) return (false, (DateTime?)null);

                    var expiry = key.GetValue("PauseUpdatesExpiryTime")?.ToString();
                    if (string.IsNullOrEmpty(expiry)) return (false, (DateTime?)null);

                    if (DateTime.TryParse(expiry, out DateTime resumeDate))
                    {
                        return (resumeDate > DateTime.Now, (DateTime?)resumeDate);
                    }
                }
                catch { }
                return (false, (DateTime?)null);
            });
        }

        public async Task<bool> InstallUpdatesAsync(List<string> updateIds)
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> DownloadAndInstallUpdatesAsync(List<string> updateIds, IProgress<double> progress)
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    return RunInSTAThread(() =>
                    {
                        Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                        if (sessionType == null) return false;

                        dynamic session = Activator.CreateInstance(sessionType)!;
                        dynamic searcher = session.CreateUpdateSearcher();
                        
                        // 0. Refresh update objects
                        dynamic searchResult = searcher.Search("IsInstalled=0 and IsHidden=0");
                        dynamic allUpdates = searchResult.Updates;
                        
                        Type? updateCollType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl");
                        if (updateCollType == null) return false;
                        dynamic updatesToInstall = Activator.CreateInstance(updateCollType)!;

                        for (int i = 0; i < allUpdates.Count; i++)
                        {
                            dynamic update = allUpdates.Item(i);
                            string uid = (string)update.Identity.UpdateID;
                            if (updateIds.Count == 0 || updateIds.Any(id => string.Equals(id, uid, StringComparison.OrdinalIgnoreCase)))
                            {
                                updatesToInstall.Add(update);
                            }
                        }

                        if (updatesToInstall.Count == 0) return true;

                        // 1. EULA Phase
                        for (int i = 0; i < updatesToInstall.Count; i++)
                        {
                            dynamic update = updatesToInstall.Item(i);
                            if (!update.EulaAccepted)
                            {
                                try { update.AcceptEula(); } catch { }
                            }
                        }

                        // 2. Download Phase (0-50%)
                        progress.Report(15);
                        dynamic downloader = session.CreateUpdateDownloader();
                        downloader.Updates = updatesToInstall;
                        dynamic downloadResult = downloader.Download();
                        progress.Report(50);

                        // 3. Install Phase (50-100%)
                        dynamic updatesToInstallFinal = Activator.CreateInstance(updateCollType)!;
                        for (int i = 0; i < updatesToInstall.Count; i++)
                        {
                            dynamic update = updatesToInstall.Item(i);
                            if (update.IsDownloaded)
                            {
                                updatesToInstallFinal.Add(update);
                            }
                        }

                        if (updatesToInstallFinal.Count == 0) return false;

                        progress.Report(75);
                        dynamic installer = session.CreateUpdateInstaller();
                        installer.Updates = updatesToInstallFinal;
                        dynamic installResult = installer.Install();
                        progress.Report(100);
                        
                        int resCode = (int)installResult.ResultCode;
                        return resCode == 2 || resCode == 3; // 2 = orcSucceeded, 3 = orcSucceededWithErrors
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WUA Direct Installation failed: {ex.Message}. Invoking OS USOClient...");
                    try
                    {
                        Process.Start(new ProcessStartInfo("usoclient.exe", "StartInteractiveInstall") { CreateNoWindow = true, UseShellExecute = false });
                        Process.Start(new ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
                        return true;
                    }
                    catch { return false; }
                }
            });
        }

        public async Task<bool> IsRebootRequiredAsync()
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    // 1. Check System Reinstallation Key (verify it has active subkeys or values)
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                    {
                        if (key != null && (key.SubKeyCount > 0 || key.ValueCount > 0)) return true;
                    }

                    // 2. Component Based Servicing (CBS) check
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
                    {
                        if (key != null && (key.SubKeyCount > 0 || key.ValueCount > 0)) return true;
                    }

                    // 3. Volatile Domain Check
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\VolatileDomain"))
                    {
                        if (key != null) return true;
                    }
                }
                catch { }
                return false;
            });
        }

        public async Task ClearRebootFlagAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired", false);
                }
                catch { }

                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", false);
                }
                catch { }
            });
        }

        public async Task RebootSystemAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
                }
                catch { }
            });
        }

        private WindowsUpdateItem MapUpdate(dynamic update, bool installed)
        {
            var item = new WindowsUpdateItem
            {
                Title = update.Title ?? "Unknown Update",
                Description = update.Description ?? "",
                IsInstalled = installed,
                IsMandatory = update.IsMandatory ?? false,
                IsDownloaded = update.IsDownloaded ?? false,
                Status = installed ? WindowsUpdateStatus.Installed : (update.IsMandatory ? WindowsUpdateStatus.Available : WindowsUpdateStatus.Optional)
            };

            try
            {
                dynamic identity = update.Identity;
                item.UpdateID = identity.UpdateID;
            }
            catch { }

            try
            {
                dynamic kbColl = update.KBArticleIDs;
                if (kbColl != null && kbColl.Count > 0)
                {
                    item.KBArticle = $"KB{kbColl.Item(0)}";
                }
                else
                {
                    var match = Regex.Match(item.Title, @"KB\d+");
                    if (match.Success) item.KBArticle = match.Value;
                }
            }
            catch { }

            try
            {
                item.Size = Convert.ToInt64(update.MaxDownloadSize);
            }
            catch { }

            try
            {
                item.ReleaseDate = Convert.ToDateTime(update.LastDeploymentChangeTime);
            }
            catch { }

            try
            {
                item.SupportUrl = update.SupportUrl ?? "";
            }
            catch { }

            return item;
        }

        public async Task<WindowsLifecycleInfo> GetWindowsLifecycleInfoAsync()
        {
            return await Task.Run(async () =>
            {
                var info = new WindowsLifecycleInfo
                {
                    Edition = "Windows 11 / 10",
                    DisplayVersion = "24H2",
                    BuildNumber = Environment.OSVersion.Version.Build.ToString(),
                    EolDate = new DateTime(2026, 10, 13)
                };

                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key != null)
                    {
                        string prodName = key.GetValue("ProductName")?.ToString() ?? "Windows";
                        string dispVer = key.GetValue("DisplayVersion")?.ToString() ?? key.GetValue("ReleaseId")?.ToString() ?? "";
                        string build = key.GetValue("CurrentBuildNumber")?.ToString() ?? Environment.OSVersion.Version.Build.ToString();

                        info.Edition = prodName;
                        info.DisplayVersion = dispVer;
                        info.BuildNumber = build;

                        int buildNum = 0;
                        int.TryParse(build, out buildNum);

                        // Windows 11 Compatibility Registry Quirk Fix:
                        // Windows 11 builds (22000+) report "Windows 10 Pro/Home" in HKLM ProductName.
                        if (buildNum >= 22000 && info.Edition.StartsWith("Windows 10"))
                        {
                            info.Edition = info.Edition.Replace("Windows 10", "Windows 11");
                        }

                        // Try loading/fetching online windows_lifecycle.json definition
                        bool resolvedOnline = await TryResolveOnlineLifecycleAsync(buildNum, info.Edition, info);

                        if (!resolvedOnline)
                        {
                            // Official Microsoft Lifecycle EOL Matrix (Offline Fallback)
                            if (buildNum >= 26200) // Windows 11 25H2 / Insider Dev
                            {
                                info.EolDate = new DateTime(2027, 10, 12);
                                if (info.Edition.Contains("Enterprise") || info.Edition.Contains("Education"))
                                    info.EolDate = new DateTime(2028, 10, 10);
                                else if (info.Edition.Contains("Server"))
                                    info.EolDate = new DateTime(2030, 10, 8);
                            }
                            else if (buildNum >= 26100) // Windows 11 24H2 / Server 2025
                            {
                                info.EolDate = new DateTime(2026, 10, 13);
                                if (info.Edition.Contains("Enterprise") || info.Edition.Contains("Education"))
                                    info.EolDate = new DateTime(2027, 10, 12);
                                else if (info.Edition.Contains("Server"))
                                    info.EolDate = new DateTime(2029, 10, 9);
                            }
                            else if (buildNum >= 22631) // Windows 11 23H2
                            {
                                info.EolDate = new DateTime(2025, 11, 11);
                                if (info.Edition.Contains("Enterprise") || info.Edition.Contains("Education"))
                                    info.EolDate = new DateTime(2026, 11, 10);
                            }
                            else if (buildNum >= 22621) // Windows 11 22H2
                            {
                                info.EolDate = new DateTime(2024, 10, 8);
                                if (info.Edition.Contains("Enterprise") || info.Edition.Contains("Education"))
                                    info.EolDate = new DateTime(2025, 10, 14);
                            }
                            else if (buildNum >= 22000) // Windows 11 21H2
                            {
                                info.EolDate = new DateTime(2023, 10, 10);
                            }
                            else if (buildNum >= 19045) // Windows 10 22H2 (Final Windows 10 release)
                            {
                                info.EolDate = new DateTime(2025, 10, 14);
                            }
                            else if (buildNum >= 19044) // Windows 10 21H2 / LTSC 2021
                            {
                                info.EolDate = prodName.Contains("LTSC") ? new DateTime(2027, 1, 12) : new DateTime(2023, 6, 13);
                            }
                            else if (buildNum >= 17763) // Windows 10 LTSC 2019 / Server 2019
                            {
                                info.EolDate = new DateTime(2029, 1, 9);
                            }
                            else // Older Windows
                            {
                                info.EolDate = new DateTime(2023, 1, 10);
                            }
                        }
                    }
                }
                catch { }

                info.DaysRemaining = (int)(info.EolDate - DateTime.Now).TotalDays;

                if (info.DaysRemaining <= 0)
                {
                    info.Status = "END OF LIFE / UNSUPPORTED";
                }
                else if (info.DaysRemaining <= 180)
                {
                    info.Status = "NEAR EOL / SERVICE END APPROACHING";
                }
                else
                {
                    info.Status = "ACTIVE SUPPORT";
                }

                return info;
            });
        }

        private async Task<bool> TryResolveOnlineLifecycleAsync(int buildNum, string prodName, WindowsLifecycleInfo info)
        {
            try
            {
                string appDataFolder = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eternal");
                global::System.IO.Directory.CreateDirectory(appDataFolder);
                string jsonPath = global::System.IO.Path.Combine(appDataFolder, "windows_lifecycle.json");

                // Download updated feed if older than 7 days or missing
                if (!global::System.IO.File.Exists(jsonPath) || (DateTime.Now - global::System.IO.File.GetLastWriteTime(jsonPath)).TotalDays > 7)
                {
                    using var http = new global::System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    string jsonOnline = await http.GetStringAsync("https://raw.githubusercontent.com/EternalSystem/LifecycleData/main/windows_lifecycle.json");
                    if (!string.IsNullOrEmpty(jsonOnline))
                    {
                        await global::System.IO.File.WriteAllTextAsync(jsonPath, jsonOnline);
                    }
                }

                if (global::System.IO.File.Exists(jsonPath))
                {
                    string jsonContent = await global::System.IO.File.ReadAllTextAsync(jsonPath);
                    var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonContent);
                    if (dict != null && dict.TryGetValue(buildNum.ToString(), out var entry))
                    {
                        string eolStr = prodName.Contains("Enterprise") || prodName.Contains("Education") 
                            ? (string)entry.eol_enterprise 
                            : (string)entry.eol_consumer;

                        if (DateTime.TryParse(eolStr, out DateTime parsedDate))
                        {
                            info.EolDate = parsedDate;
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}

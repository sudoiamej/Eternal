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
            return await Task.Run<bool>(async () =>
            {
                try
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
                        if (updateIds.Contains((string)update.Identity.UpdateID))
                            updatesToInstall.Add(update);
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
                    dynamic downloader = session.CreateUpdateDownloader();
                    downloader.Updates = updatesToInstall;
                    dynamic downloadJob = downloader.BeginDownload(null, null, null);

                    while (!downloadJob.IsCompleted)
                    {
                        try 
                        {
                            dynamic dlProgress = downloadJob.GetProgress();
                            progress.Report(dlProgress.PercentComplete * 0.5);
                        } catch { }
                        await Task.Delay(1000);
                    }
                    downloader.EndDownload(downloadJob);

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

                    dynamic installer = session.CreateUpdateInstaller();
                    installer.Updates = updatesToInstallFinal;
                    dynamic installJob = installer.BeginInstall(null, null, null);

                    while (!installJob.IsCompleted)
                    {
                        try
                        {
                            dynamic instProgress = installJob.GetProgress();
                            progress.Report(50 + (instProgress.PercentComplete * 0.5));
                        } catch { }
                        await Task.Delay(1000);
                    }
                    
                    dynamic installResult = installer.EndInstall(installJob);
                    progress.Report(100);
                    
                    return (int)installResult.ResultCode == 2; // 2 = orCucceeded
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Update installation failed: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> IsRebootRequiredAsync()
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    // 1. Check System Reinstallation Key
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                    {
                        if (key != null) return true;
                    }

                    // 2. Check WUA API
                    Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                    if (sessionType != null)
                    {
                        dynamic session = Activator.CreateInstance(sessionType)!;
                        dynamic searcher = session.CreateUpdateSearcher();
                        // WUA API doesn't have a direct "GlobalIsRestartRequired", 
                        // but individual update results do.
                    }

                    // 3. Component Based Servicing (CBS) check
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
                    {
                        if (key != null) return true;
                    }
                }
                catch { }
                return false;
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
                // Attempt to extract KB article number from Title (e.g., "KB5034765")
                var match = Regex.Match(item.Title, @"KB\d+");
                if (match.Success) item.KBArticle = match.Value;
                
                dynamic identity = update.Identity;
                item.UpdateID = identity.UpdateID;
            }
            catch { }

            return item;
        }
    }
}

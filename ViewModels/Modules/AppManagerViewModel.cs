using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class AppManagerViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<InstalledAppModel> _apps = new();
        [ObservableProperty] private ObservableCollection<InstalledAppModel> _filteredApps = new();
        [ObservableProperty] private InstalledAppModel? _selectedApp;
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _totalAppsCount = "0 Installed Applications";

        public AppManagerViewModel()
        {
            _ = ScanInstalledAppsAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public async Task ScanInstalledAppsAsync()
        {
            StatusText = "Scanning Windows Registry for installed software...";
            await Task.Run(() =>
            {
                var list = new List<InstalledAppModel>();

                // 64-bit & 32-bit registry uninstall paths
                string[] regPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in regPaths)
                {
                    ScanRegistryKey(Registry.LocalMachine, path, list);
                    ScanRegistryKey(Registry.CurrentUser, path, list);
                }

                var distinctList = list
                    .Where(a => !string.IsNullOrWhiteSpace(a.DisplayName))
                    .GroupBy(a => a.DisplayName)
                    .Select(g => g.First())
                    .OrderBy(a => a.DisplayName)
                    .ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Apps = new ObservableCollection<InstalledAppModel>(distinctList);
                    ApplyFilter();
                    TotalAppsCount = $"{Apps.Count} Installed Program(s)";
                    StatusText = $"Audit complete at {DateTime.Now:HH:mm:ss}";
                });
            });
        }

        [RelayCommand]
        public void UninstallApp(InstalledAppModel? app)
        {
            if (app == null || string.IsNullOrWhiteSpace(app.UninstallString)) return;

            try
            {
                string cmd = app.UninstallString;
                string exe = cmd;
                string args = "";

                if (cmd.StartsWith("\""))
                {
                    int idx = cmd.IndexOf("\"", 1);
                    if (idx > 0)
                    {
                        exe = cmd.Substring(1, idx - 1);
                        args = cmd.Substring(idx + 1).Trim();
                    }
                }
                else
                {
                    int idx = cmd.IndexOf(" ");
                    if (idx > 0)
                    {
                        exe = cmd.Substring(0, idx);
                        args = cmd.Substring(idx + 1).Trim();
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                });
                StatusText = $"Launched uninstaller for {app.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusText = $"Uninstall Error: {ex.Message}";
            }
        }

        private void ScanRegistryKey(RegistryKey rootKey, string path, List<InstalledAppModel> list)
        {
            try
            {
                using var key = rootKey.OpenSubKey(path);
                if (key == null) return;

                foreach (string subkeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subkey = key.OpenSubKey(subkeyName);
                        if (subkey == null) continue;

                        string name = subkey.GetValue("DisplayName")?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        string pub = subkey.GetValue("Publisher")?.ToString() ?? "Unknown";
                        string ver = subkey.GetValue("DisplayVersion")?.ToString() ?? "N/A";
                        string date = subkey.GetValue("InstallDate")?.ToString() ?? "N/A";
                        string uninst = subkey.GetValue("UninstallString")?.ToString() ?? "";

                        list.Add(new InstalledAppModel
                        {
                            DisplayName = name,
                            Publisher = pub,
                            DisplayVersion = ver,
                            InstallDate = date,
                            UninstallString = uninst,
                            Architecture = path.Contains("WOW6432Node") ? "x86" : "x64"
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredApps = new ObservableCollection<InstalledAppModel>(Apps);
            }
            else
            {
                var q = SearchQuery.ToLower();
                var filtered = Apps.Where(a => a.DisplayName.ToLower().Contains(q) || a.Publisher.ToLower().Contains(q)).ToList();
                FilteredApps = new ObservableCollection<InstalledAppModel>(filtered);
            }
        }
    }
}

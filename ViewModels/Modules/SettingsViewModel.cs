using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Diagnostics;

namespace Eternal.ViewModels.Modules
{
    public partial class SettingsViewModel : BaseViewModel
    {
        public MainViewModel Main { get; }
        private readonly ISettingsService _settingsService;
        private readonly IUpdateService _updateService;

        [ObservableProperty] private AppSettings _settings;
        [ObservableProperty] private string _appVersion = "3.1.0";
        [ObservableProperty] private string _lastScanTime = "N/A";
        [ObservableProperty] private string _machineId = "Unknown";

        private int _versionClickCount = 0;

        [RelayCommand]
        private void VersionClicked()
        {
            _versionClickCount++;
            if (_versionClickCount >= 5)
            {
                _versionClickCount = 0;
                var aboutWin = new Eternal.Views.Helpers.AboutVersionWindow();
                aboutWin.Owner = System.Windows.Application.Current.MainWindow;
                aboutWin.ShowDialog();
            }
        }

        public string SelectedTheme
        {
            get => Settings.Theme;
            set
            {
                if (Settings.Theme != value)
                {
                    Settings.Theme = value;
                    OnPropertyChanged();
                    Main.ApplyThemeColor(); // This will trigger theme refresh
                }
            }
        }

        public string SelectedGradiency
        {
            get => Settings.NewUiGradiency;
            set
            {
                if (Settings.NewUiGradiency != value)
                {
                    Settings.NewUiGradiency = value;
                    OnPropertyChanged();
                    Main.ApplyThemeColor();
                }
            }
        }

        public SettingsViewModel(MainViewModel main, ISettingsService settingsService, IUpdateService updateService)
        {
            Main = main;
            _settingsService = settingsService;
            _updateService = updateService;
            Settings = _settingsService.Current;

            // Generate/Fetch Fingerprint
            var fingerprintService = new MachineFingerprintService();
            MachineId = fingerprintService.GetFingerprint();
            Settings.MachineFingerprint = MachineId;
        }
        [RelayCommand]
        private void SaveConfig()
        {
            UpdateStartupRegistration();
            _settingsService.Save();
            Eternal.Views.Helpers.CustomNotificationWindow.Show("Configuration saved successfully to local app data.", "Settings", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Success);
        }

        [RelayCommand]
        private void SetThemeColor(string hexColor)
        {
            Settings.ThemeAccentColor = hexColor;
            Main.ApplyThemeColor();
        }

        [RelayCommand]
        private void ResetDefaults()
        {
            var defaults = new AppSettings();
            Settings.RefreshFrequency = defaults.RefreshFrequency;
            Settings.PreloadOnStartup = defaults.PreloadOnStartup;
            Settings.IsAdvancedMode = defaults.IsAdvancedMode;
            Settings.PollingProfile = defaults.PollingProfile;
            Settings.RunAtStartup = defaults.RunAtStartup;
            Settings.MinimizeToTray = defaults.MinimizeToTray;
            Settings.ExportFolderPath = defaults.ExportFolderPath;
            Settings.WmiTimeoutSeconds = defaults.WmiTimeoutSeconds;
            Settings.IsVerboseLoggingEnabled = defaults.IsVerboseLoggingEnabled;
            Settings.ThemeAccentColor = defaults.ThemeAccentColor;
            
            Settings.IsStartupLockEnabled = defaults.IsStartupLockEnabled;
            Settings.StartupLockPin = defaults.StartupLockPin;
            Settings.LockoutEnd = null;
            Settings.FailedAttemptsCount = 0;
            Settings.CurrentLockoutMinutes = 0;
            
            Main.ApplyThemeColor();
            UpdateStartupRegistration();
            _settingsService.Save();
            Eternal.Views.Helpers.CustomNotificationWindow.Show("Settings restored to factory defaults.", "Settings", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Warning);
        }

        [RelayCommand]
        private void ClearAllData()
        {
            var confirmWin = new Eternal.Views.Helpers.ResetDataConfirmWindow();
            confirmWin.Owner = System.Windows.Application.Current.MainWindow;

            if (confirmWin.ShowDialog() == true)
            {
                try
                {
                    // 1. Reset settings to default in memory
                    Settings = new AppSettings();
                    
                    // 2. Clear startup registration
                    Settings.RunAtStartup = false;
                    UpdateStartupRegistration();

                    // 3. Delete the local app data directory
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string folder = Path.Combine(appData, "EternalAnalytics");
                    
                    if (Directory.Exists(folder))
                    {
                        // Note: settings.json is usually in use if we don't handle it carefully, 
                        // but since we are about to exit, we can try to delete everything else
                        // and the file itself if possible.
                        foreach (var file in Directory.GetFiles(folder))
                        {
                            try { File.Delete(file); } catch { }
                        }
                        foreach (var dir in Directory.GetDirectories(folder))
                        {
                            try { Directory.Delete(dir, true); } catch { }
                        }
                    }

                    Eternal.Views.Helpers.CustomNotificationWindow.Show("Data purge complete. The application will now exit.", "Reset Successful", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Success);
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Eternal.Views.Helpers.CustomNotificationWindow.Show($"Purge failed: {ex.Message}", "Reset Error", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Error);
                }
            }
        }

        [RelayCommand]
        private void ChangeStartupPin()
        {
            var editWin = new Eternal.Views.Helpers.StartupPinEditWindow();
            editWin.Owner = System.Windows.Application.Current.MainWindow;
            
            if (editWin.ShowDialog() == true)
            {
                Settings.StartupLockPin = editWin.NewPin;
                _settingsService.Save();
                Eternal.Views.Helpers.CustomNotificationWindow.Show("Startup Access Code updated successfully.", "Security", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Success);
            }
        }

        [RelayCommand]
        private async Task CheckForUpdates()
        {
            await Main.CheckForUpdatesAsync(true);
        }

        [RelayCommand]
        private void SelectExportFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                Settings.ExportFolderPath = dialog.FolderName;
            }
        }

        private void UpdateStartupRegistration()
        {
            try
            {
                string path = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(path)) return;

                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (Settings.RunAtStartup)
                {
                    key?.SetValue("EternalSystemIntelligence", $"\"{path}\"");
                }
                else
                {
                    key?.DeleteValue("EternalSystemIntelligence", false);
                }
            }
            catch { }
        }
    }
}

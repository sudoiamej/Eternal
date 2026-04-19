using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;
using Microsoft.Win32;
using System;
using System.IO;
using System.Diagnostics;

namespace Eternal.ViewModels.Modules
{
    public partial class SettingsViewModel : ObservableObject
    {
        public MainViewModel Main { get; }
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;
        private readonly IUpdateService _updateService;

        [ObservableProperty] private AppSettings _settings;
        [ObservableProperty] private string _appVersion = "2.5.0-M3";
        [ObservableProperty] private string _lastScanTime = "N/A";
        [ObservableProperty] private string _machineId = "Unknown";

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
            System.Windows.MessageBox.Show("Configuration saved successfully to local app data.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
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
            System.Windows.MessageBox.Show("Settings restored to factory defaults.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                System.Windows.MessageBox.Show("Startup Access Code updated successfully.", "Security", MessageBoxButton.OK, MessageBoxImage.Information);
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

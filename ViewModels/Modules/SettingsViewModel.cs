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

        [ObservableProperty] private AppSettings _settings;
        [ObservableProperty] private string _appVersion = "2.0.0P-CREATOR";
        [ObservableProperty] private string _lastScanTime = "N/A";

        public SettingsViewModel(MainViewModel main, ISettingsService settingsService)
        {
            Main = main;
            _settingsService = settingsService;
            Settings = _settingsService.Current;
            
            // Try to resolve logging service from Main if possible, or just skip for now 
            // In a real app we'd use DI, but here we can try to find it or pass it.
            // Since we don't have easy DI here, let's just use it if we can.
        }

        [RelayCommand]
        private void SaveConfig()
        {
            UpdateStartupRegistration();
            _settingsService.Save();
            MessageBox.Show("Configuration saved successfully to local app data.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
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
            
            UpdateStartupRegistration();
            _settingsService.Save();
            MessageBox.Show("Settings restored to factory defaults.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
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

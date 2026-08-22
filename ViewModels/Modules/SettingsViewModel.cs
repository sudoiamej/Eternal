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
        [ObservableProperty] private string _appVersion = "3.5.0 (Stable)";
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

        public DashboardLayoutMode SelectedDashboardLayoutMode
        {
            get => Settings.DashboardLayoutMode;
            set
            {
                if (Settings.DashboardLayoutMode != value)
                {
                    Settings.DashboardLayoutMode = value;
                    OnPropertyChanged();
                    _settingsService.Save();
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
            if (!string.IsNullOrWhiteSpace(hexColor))
            {
                Settings.ThemeAccentColor = hexColor;
                OnPropertyChanged(nameof(Settings));
                Main.ApplyThemeColor();
                _settingsService.Save();
            }
        }

        public double SelectedFontScale
        {
            get => Settings.FontAdjustmentScale;
            set
            {
                double constrainedVal = Math.Max(1.0, value);
                if (Settings.FontAdjustmentScale != constrainedVal)
                {
                    Settings.FontAdjustmentScale = constrainedVal;
                    OnPropertyChanged();
                    Main.UpdateFontScale();
                }
            }
        }

        public double SelectedWindowScale
        {
            get => Settings.WindowScale;
            set
            {
                double constrainedVal = Math.Max(1.0, value);
                if (Settings.WindowScale != constrainedVal)
                {
                    Settings.WindowScale = constrainedVal;
                    OnPropertyChanged();
                    Main.UpdateWindowScale();
                }
            }
        }

        [RelayCommand]
        private void ResetDefaults()
        {
            var defaults = new AppSettings();
            Settings.RefreshFrequency = defaults.RefreshFrequency;
            Settings.IsAdvancedMode = defaults.IsAdvancedMode;
            Settings.PollingProfile = defaults.PollingProfile;
            Settings.RunAtStartup = defaults.RunAtStartup;
            Settings.MinimizeToTray = defaults.MinimizeToTray;
            Settings.ExportFolderPath = defaults.ExportFolderPath;
            Settings.WmiTimeoutSeconds = defaults.WmiTimeoutSeconds;
            Settings.IsVerboseLoggingEnabled = defaults.IsVerboseLoggingEnabled;
            Settings.ThemeAccentColor = "#0078D4";
            Settings.FontAdjustmentScale = defaults.FontAdjustmentScale;
            Settings.WindowScale = defaults.WindowScale;
            Settings.IsSidebarExpanded = defaults.IsSidebarExpanded;
            Settings.IsAutoUpdateEnabled = defaults.IsAutoUpdateEnabled;
            
            Settings.IsStartupLockEnabled = defaults.IsStartupLockEnabled;
            Settings.LockoutEnd = null;
            Settings.FailedAttemptsCount = 0;
            Settings.CurrentLockoutMinutes = 0;
            
            Main.ApplyThemeColor();
            Main.UpdateFontScale();
            Main.UpdateWindowScale();
            UpdateStartupRegistration();
            _settingsService.Save();

            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(SelectedWindowScale));
            OnPropertyChanged(nameof(SelectedFontScale));

            Eternal.Views.Helpers.CustomNotificationWindow.Show("Settings restored to factory defaults.", "Settings", Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Warning);
        }

        [RelayCommand]
        private void ResetCalibrationData()
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all hardware baseline calibration profiles, custom modes, and telemetry caches?\n\nThis will re-trigger the initial hardware calibration scan upon next application startup.",
                "Reset Calibration & Hardware Baseline",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Settings.IsFirstRun = true;
                Settings.UseLegacyUI = false;
                Settings.FontAdjustmentScale = 1.0;
                Settings.WindowScale = 1.0;
                Settings.PollingProfile = "Balanced";
                Settings.IsStartupLockEnabled = false;
                Settings.IsAdvancedMode = false;
                Settings.IsVerboseLoggingEnabled = false;

                _settingsService.Save();

            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(SelectedWindowScale));
            OnPropertyChanged(nameof(SelectedFontScale));

                Eternal.Views.Helpers.CustomNotificationWindow.Show(
                    "Hardware calibration & workstation state reset successfully. Calibration scan will run on next boot.", 
                    "Calibration Reset", 
                    Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Success);
            }
        }

        [RelayCommand]
        private void ClearAllData()
        {
            ResetCalibrationData();
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

                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (Settings.RunAtStartup)
                    {
                        key.SetValue("EternalSystemIntelligence", $"\"{path}\"");
                    }
                    else
                    {
                        key.DeleteValue("EternalSystemIntelligence", false);
                    }
                }
            }
            catch { }
        }
    }
}

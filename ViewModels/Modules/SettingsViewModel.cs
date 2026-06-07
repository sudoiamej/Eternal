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
        [ObservableProperty] private string _appVersion = "3.2.0";
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
            Settings.ThemeAccentColor = hexColor;
            Main.ApplyThemeColor();
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
            Settings.PreloadOnStartup = defaults.PreloadOnStartup;
            Settings.IsAdvancedMode = defaults.IsAdvancedMode;
            Settings.PollingProfile = defaults.PollingProfile;
            Settings.RunAtStartup = defaults.RunAtStartup;
            Settings.MinimizeToTray = defaults.MinimizeToTray;
            Settings.ExportFolderPath = defaults.ExportFolderPath;
            Settings.WmiTimeoutSeconds = defaults.WmiTimeoutSeconds;
            Settings.IsVerboseLoggingEnabled = defaults.IsVerboseLoggingEnabled;
            Settings.ThemeAccentColor = defaults.ThemeAccentColor;
            Settings.FontAdjustmentScale = defaults.FontAdjustmentScale;
            Settings.WindowScale = defaults.WindowScale;
            
            Settings.IsStartupLockEnabled = defaults.IsStartupLockEnabled;
            Settings.StartupLockPin = defaults.StartupLockPin;
            Settings.LockoutEnd = null;
            Settings.FailedAttemptsCount = 0;
            Settings.CurrentLockoutMinutes = 0;
            
            Main.ApplyThemeColor();
            Main.UpdateFontScale();
            Main.UpdateWindowScale();
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
                var mainWindow = System.Windows.Application.Current.MainWindow as Eternal.Views.NeumorphicMainWindow;
                if (mainWindow != null)
                {
                    mainWindow.ShowFactoryResetPromptFromSettings();
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

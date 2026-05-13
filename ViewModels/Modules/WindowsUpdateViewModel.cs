using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class WindowsUpdateViewModel : BaseViewModel
    {
        private readonly IOsUpdateService _updateService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private bool _isChecking;
        [ObservableProperty] private bool _isInstalling;
        [ObservableProperty] private double _installProgress;
        [ObservableProperty] private string _estimatedTimeRemaining = string.Empty;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private bool _isRebootRequired;
        [ObservableProperty] private string _statusMessage = "Up to date";
        [ObservableProperty] private DateTime? _pauseUntil;
        [ObservableProperty] private ObservableCollection<WindowsUpdateItem> _availableUpdates = new();
        [ObservableProperty] private ObservableCollection<WindowsUpdateItem> _installedUpdates = new();

        public WindowsUpdateViewModel(IOsUpdateService updateService, ILoggingService loggingService)
        {
            _updateService = updateService;
            _loggingService = loggingService;
            
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await RefreshStatusAsync();
            await LoadUpdatesAsync();
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            if (IsChecking || IsInstalling) return;
            
            IsChecking = true;
            StatusMessage = "Checking for updates...";
            _loggingService.Log("OS Update: Checking for available patches...");

            try
            {
                var updates = await _updateService.GetAvailableUpdatesAsync();
                AvailableUpdates.Clear();
                foreach (var update in updates) AvailableUpdates.Add(update);

                StatusMessage = AvailableUpdates.Any() ? $"{AvailableUpdates.Count} updates available" : "You're up to date";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error checking for updates";
                _loggingService.Log($"OS Update Error: {ex.Message}");
            }
            finally
            {
                IsChecking = false;
            }
        }

        [RelayCommand]
        private async Task LoadUpdatesAsync()
        {
            try
            {
                var history = await _updateService.GetInstalledUpdatesAsync();
                InstalledUpdates.Clear();
                foreach (var update in history) InstalledUpdates.Add(update);
            }
            catch { }
        }

        [RelayCommand]
        private async Task PauseUpdatesAsync(string daysStr)
        {
            if (int.TryParse(daysStr, out int days))
            {
                _loggingService.Log($"OS Update: Pausing updates for {days} days.");
                if (await _updateService.PauseUpdatesAsync(days))
                {
                    await RefreshStatusAsync();
                }
            }
        }

        [RelayCommand]
        private async Task ResumeUpdatesAsync()
        {
            _loggingService.Log("OS Update: Resuming updates.");
            if (await _updateService.ResumeUpdatesAsync())
            {
                await RefreshStatusAsync();
            }
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            await _updateService.InstallUpdatesAsync(new List<string>());
        }

        [RelayCommand]
        private async Task InstallUpdateAsync(WindowsUpdateItem update)
        {
            if (IsInstalling) return;
            await ExecuteInstallAsync(new List<string> { update.UpdateID });
        }

        [RelayCommand]
        private async Task InstallAllAsync()
        {
            if (IsInstalling || !AvailableUpdates.Any()) return;
            await ExecuteInstallAsync(AvailableUpdates.Select(u => u.UpdateID).ToList());
        }

        [RelayCommand]
        private async Task RebootSystemAsync()
        {
            var result = System.Windows.MessageBox.Show("The system will restart immediately. Ensure all work is saved.", "Restart Required", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.OK)
            {
                _loggingService.Log("OS Update: User initiated system restart.");
                await _updateService.RebootSystemAsync();
            }
        }

        private async Task ExecuteInstallAsync(List<string> ids)
        {
            IsInstalling = true;
            InstallProgress = 0;
            EstimatedTimeRemaining = "Calculating time remaining...";
            StatusMessage = "Installing updates...";
            _loggingService.Log($"OS Update: Starting installation of {ids.Count} updates.");

            DateTime startTime = DateTime.Now;

            try
            {
                var progress = new Progress<double>(p => 
                {
                    InstallProgress = p;
                    
                    if (p > 5) // Wait for a stable average
                    {
                        var elapsed = DateTime.Now - startTime;
                        var totalEstimated = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / (p / 100));
                        var remaining = totalEstimated - elapsed;

                        if (remaining.TotalMinutes > 1)
                            EstimatedTimeRemaining = $"About {Math.Ceiling(remaining.TotalMinutes)} minutes remaining";
                        else if (remaining.TotalSeconds > 10)
                            EstimatedTimeRemaining = $"About {Math.Ceiling(remaining.TotalSeconds / 10) * 10} seconds remaining";
                        else
                            EstimatedTimeRemaining = "Finishing up...";
                    }
                });

                bool success = await _updateService.DownloadAndInstallUpdatesAsync(ids, progress);
                if (success)
                {
                    StatusMessage = "Updates installed. Reboot may be required.";
                    await InitializeAsync();
                }
                else
                {
                    StatusMessage = "Installation failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error during installation";
                _loggingService.Log($"OS Update Install Error: {ex.Message}");
            }
            finally
            {
                IsInstalling = false;
                InstallProgress = 0;
                EstimatedTimeRemaining = string.Empty;
            }
        }

        private async Task RefreshStatusAsync()
        {
            var status = await _updateService.GetPauseStatusAsync();
            IsPaused = status.IsPaused;
            PauseUntil = status.ResumeDate;
            IsRebootRequired = await _updateService.IsRebootRequiredAsync();
            
            if (IsRebootRequired)
            {
                StatusMessage = "System restart recommended to complete updates.";
            }
        }
    }
}

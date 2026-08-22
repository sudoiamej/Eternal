using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class TuningViewModel : BaseViewModel
    {
        private readonly ITuningService _tuningService;

        public ObservableCollection<SystemTweak> Tweaks { get; } = new ObservableCollection<SystemTweak>();
        
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Ready";

        public TuningViewModel(ITuningService tuningService)
        {
            _tuningService = tuningService;
            LoadTweaksCommand = new AsyncRelayCommand(LoadTweaksAsync);
        }

        public IAsyncRelayCommand LoadTweaksCommand { get; }

        public override void ReleaseMemory()
        {
            Tweaks.Clear();
        }

        private async Task LoadTweaksAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Analyzing system configuration...";
            try
            {
                var tweaks = await _tuningService.GetTweaksAsync();
                Tweaks.Clear();
                foreach (var t in tweaks) Tweaks.Add(t);
                StatusMessage = "Analysis complete.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Analysis failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task ToggleTweak(SystemTweak tweak)
        {
            if (tweak == null || IsBusy) return;

            IsBusy = true;
            bool success;
            bool wasApplied = tweak.IsApplied;

            try
            {
                if (wasApplied)
                {
                    StatusMessage = $"Reverting {tweak.Name}...";
                    success = await _tuningService.UndoTweakAsync(tweak.Id);
                }
                else
                {
                    StatusMessage = $"Applying {tweak.Name}...";
                    success = await _tuningService.ApplyTweakAsync(tweak.Id);
                }

                if (success)
                {
                    tweak.IsApplied = !wasApplied;
                    StatusMessage = $"{tweak.Name} {(tweak.IsApplied ? "applied" : "reverted")} successfully.";
                    
                    // Force a refresh of the individual item state if needed, or reload all
                    // For now, reload all to be safe and ensure CheckIsApplied runs again
                    await LoadTweaksAsync();
                }
                else
                {
                    StatusMessage = "Operation failed. Elevation (Admin) is likely required.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task ApplyGamingPreset()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "Applying Low-Latency Gaming Profile...";

            try
            {
                await _tuningService.ApplyTweakAsync("perf_game_dvr");
                await _tuningService.ApplyTweakAsync("perf_mmcss_priority");
                await _tuningService.ApplyTweakAsync("perf_network_nagle");
                await LoadTweaksAsync();
                StatusMessage = "Gaming Low-Latency Profile applied successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to apply gaming profile: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task CreateRestorePoint()
        {
            StatusMessage = "Creating system restore point...";
            bool success = await _tuningService.CreateRestorePointAsync("Eternal Guardian Optimization");
            StatusMessage = success ? "Restore point created successfully." : "Failed to create restore point.";
        }
    }
}

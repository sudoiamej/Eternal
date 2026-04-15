using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class TuningViewModel : ObservableObject
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

        private async Task LoadTweaksAsync()
        {
            IsBusy = true;
            StatusMessage = "Analyzing system configuration...";
            try
            {
                var tweaks = await _tuningService.GetTweaksAsync();
                Tweaks.Clear();
                foreach (var t in tweaks) Tweaks.Add(t);
                StatusMessage = "Analysis complete.";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task ToggleTweak(SystemTweak tweak)
        {
            if (tweak == null) return;

            bool success;
            if (tweak.IsApplied)
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
                StatusMessage = "Tweak updated successfully.";
                await LoadTweaksAsync(); // Refresh state
            }
            else
            {
                StatusMessage = "Failed to update tweak. Elevation may be required.";
            }
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.System;
using System.Threading.Tasks;

namespace Eternal.ViewModels.Modules
{
    public partial class FlagsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IToastService _toastService;

        public AppSettings Flags => _settingsService.Current;

        [ObservableProperty] private bool _enableWmiPolling;
        [ObservableProperty] private bool _bypassAdminCheck;
        [ObservableProperty] private bool _forcePeMode;
        [ObservableProperty] private bool _safeExecutionMode;
        [ObservableProperty] private bool _verboseServiceLogging;
        [ObservableProperty] private bool _simulateUpdateFailure;
        [ObservableProperty] private bool _useNativeMemoryPolling;

        public FlagsViewModel(ISettingsService settingsService, IToastService toastService)
        {
            _settingsService = settingsService;
            _toastService = toastService;
            
            LoadFlags();
        }

        private void LoadFlags()
        {
            var f = _settingsService.Current;
            EnableWmiPolling = f.EnableWmiPolling;
            BypassAdminCheck = f.BypassAdminCheck;
            ForcePeMode = f.ForcePeMode;
            SafeExecutionMode = f.SafeExecutionMode;
            VerboseServiceLogging = f.VerboseServiceLogging;
            SimulateUpdateFailure = f.SimulateUpdateFailure;
            UseNativeMemoryPolling = f.UseNativeMemoryPolling;
        }

        [RelayCommand]
        private void SaveFlags()
        {
            var f = _settingsService.Current;
            f.EnableWmiPolling = EnableWmiPolling;
            f.BypassAdminCheck = BypassAdminCheck;
            f.ForcePeMode = ForcePeMode;
            f.SafeExecutionMode = SafeExecutionMode;
            f.VerboseServiceLogging = VerboseServiceLogging;
            f.SimulateUpdateFailure = SimulateUpdateFailure;
            f.UseNativeMemoryPolling = UseNativeMemoryPolling;

            _settingsService.Save();
            _toastService.ShowSuccess("System flags updated. Some changes may require restart.");
        }

        [RelayCommand]
        private void ResetFlags()
        {
            EnableWmiPolling = true;
            BypassAdminCheck = false;
            ForcePeMode = false;
            SafeExecutionMode = true;
            VerboseServiceLogging = false;
            SimulateUpdateFailure = false;
            UseNativeMemoryPolling = true;
            
            SaveFlags();
        }
    }
}

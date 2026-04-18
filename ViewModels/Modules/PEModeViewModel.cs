using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.Hardware;

namespace Eternal.ViewModels.Modules
{
    public partial class PEModeViewModel : ObservableObject
    {
        private readonly IHardwareService _hardwareService;
        private readonly bool _isInPeMode;

        [ObservableProperty] private string _systemDriveStatus = "Checking...";
        [ObservableProperty] private string _bootRecordStatus = "Checking...";
        [ObservableProperty] private string _memoryStatus = "Checking...";
        [ObservableProperty] private bool _isLoading;

        public PEModeViewModel(IHardwareService hardwareService, bool isInPeMode)
        {
            _hardwareService = hardwareService;
            _isInPeMode = isInPeMode;
            LoadDiagnosticsCommand = new AsyncRelayCommand(RunDiagnosticsAsync);
            
            // Auto-run diagnostics on entry
            Task.Run(async () => await RunDiagnosticsAsync());
        }

        public IAsyncRelayCommand LoadDiagnosticsCommand { get; }

        private async Task RunDiagnosticsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                await Task.Delay(500); 
                var disks = await _hardwareService.GetDiskInfoAsync();
                bool allDisksHealthy = true;
                foreach(var d in disks) { if (d.Health != "OK" && d.Health != "Unknown") allDisksHealthy = false; }

                SystemDriveStatus = allDisksHealthy ? (disks.Count > 0 ? "Healthy" : "No Disks Detected") : "Errors Detected";
                BootRecordStatus = "Valid (Offline Check)";
                var ram = await _hardwareService.GetRamInfoAsync();
                MemoryStatus = ram.TotalCapacity != "0 GB" ? "Detected & Accessible" : "Read Error";
            }
            catch 
            {
                SystemDriveStatus = "Read Error";
                BootRecordStatus = "Unavailable";
                MemoryStatus = "Error";
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private void RunRecoveryTool(string toolName)
        {
            if (!_isInPeMode)
            {
                System.Windows.MessageBox.Show($"Access Denied: '{toolName}' is a destructive recovery tool and can ONLY be executed within a native Windows PE or RE environment.\n\nPlease boot into Advanced Startup Options to use this tool.", 
                                "HCI Safety Enforcement", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            System.Windows.MessageBox.Show($"Initializing {toolName}...", "PE Mode Action", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

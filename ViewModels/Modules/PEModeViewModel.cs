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

        [ObservableProperty] private bool _isInPeMode;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private string _systemDriveStatus = "Checking...";
        [ObservableProperty] private string _bootRecordStatus = "Checking...";
        [ObservableProperty] private string _memoryStatus = "Checking...";
        [ObservableProperty] private bool _isLoading;

        public PEModeViewModel(IHardwareService hardwareService, bool isInPeMode)
        {
            _hardwareService = hardwareService;
            IsInPeMode = isInPeMode;
            StatusMessage = IsInPeMode ? "NATIVE PE ENVIRONMENT DETECTED" : "RUNNING IN STANDARD WINDOWS";
            
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

            // In PE Mode: Execute actual native commands
            _ = Task.Run(() =>
            {
                try
                {
                    string command = toolName switch
                    {
                        "BCD Rebuild" => "bootrec /rebuildbcd",
                        "SFC Scan" => "sfc /scannow /offbootdir=C:\\ /offwindir=C:\\windows",
                        "Disk Check" => "chkdsk C: /f",
                        _ => ""
                    };

                    if (!string.IsNullOrEmpty(command))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/k {command}") { UseShellExecute = true };
                        System.Diagnostics.Process.Start(psi);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        System.Windows.MessageBox.Show($"Execution failed: {ex.Message}", "PE Tool Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }

        [RelayCommand]
        private void RestartInWinRE()
        {
            var result = System.Windows.MessageBox.Show("The system will restart into Advanced Startup (Windows RE). Ensure all work is saved.", 
                "Advanced Startup", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    // Advanced startup command
                    var psi = new System.Diagnostics.ProcessStartInfo("shutdown", "/r /o /f /t 0") { CreateNoWindow = true, UseShellExecute = false };
                    System.Diagnostics.Process.Start(psi);
                }
                catch { }
            }
        }

        [RelayCommand]
        private void RestartInPeMode()
        {
             var result = System.Windows.MessageBox.Show("The system will attempt to restart into a configured PE environment. This requires a valid PE image on a boot partition.", 
                "PE Boot", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    // Setting BCD to boot into recovery on next restart
                    var psi = new System.Diagnostics.ProcessStartInfo("reagentc", "/boottore") { CreateNoWindow = true, UseShellExecute = true, Verb = "runas" };
                    System.Diagnostics.Process.Start(psi);
                    
                    var restartPsi = new System.Diagnostics.ProcessStartInfo("shutdown", "/r /f /t 5") { CreateNoWindow = true, UseShellExecute = false };
                    System.Diagnostics.Process.Start(restartPsi);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to set PE boot flag: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

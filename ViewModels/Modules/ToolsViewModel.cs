using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class ToolsViewModel : BaseViewModel
    {
        private readonly IToolkitService _toolkitService;

        public ToolsViewModel(IToolkitService toolkitService)
        {
            _toolkitService = toolkitService;
        }

        [RelayCommand]
        private async Task FlushDns()
        {
            bool success = await _toolkitService.FlushDnsAsync();
            System.Windows.MessageBox.Show(success ? "DNS Cache Flushed Successfully" : "Failed to Flush DNS");
        }

        [RelayCommand]
        private async Task ClearTemp()
        {
            long bytes = await _toolkitService.ClearTempFilesAsync();
            System.Windows.MessageBox.Show($"Cleared {(bytes / 1024 / 1024)} MB of temporary files.");
        }

        [RelayCommand]
        private async Task RebuildIcons()
        {
            bool success = await _toolkitService.RebuildIconCacheAsync();
            System.Windows.MessageBox.Show(success ? "Icon Cache Rebuild Triggered" : "Failed to rebuild cache. Ensure you have proper permissions.");
        }

        [RelayCommand]
        private async Task ResetNetwork()
        {
            bool success = await _toolkitService.ResetNetworkStackAsync();
            System.Windows.MessageBox.Show(success ? "Network Stack Reset. A system restart is highly recommended." : "Operation cancelled or failed.");
        }

        [RelayCommand]
        private async Task RepairSystemFiles()
        {
            bool success = await _toolkitService.RunSfcScanAsync();
            System.Windows.MessageBox.Show(success ? "System File Checker Completed." : "Operation cancelled or failed.");
        }

        [RelayCommand]
        private async Task RunDism()
        {
            bool success = await _toolkitService.RunDismRepairAsync();
            System.Windows.MessageBox.Show(success ? "DISM Restore Health Completed." : "Operation cancelled or failed.");
        }

        [RelayCommand]
        private async Task ResetWindowsUpdate()
        {
            bool success = await _toolkitService.ResetWindowsUpdateAsync();
            System.Windows.MessageBox.Show(success ? "Windows Update Components Reset Successfully." : "Failed to reset components.");
        }

        [RelayCommand]
        private async Task ClearEventLogs()
        {
            bool success = await _toolkitService.ClearEventLogsAsync();
            System.Windows.MessageBox.Show(success ? "All System Event Logs Cleared." : "Failed to clear event logs.");
        }

        [RelayCommand]
        private async Task OptimizeBoot()
        {
            bool success = await _toolkitService.OptimizeBootPerformanceAsync();
            System.Windows.MessageBox.Show(success ? "Boot Performance Optimization Triggered." : "Failed to trigger optimization.");
        }
    }
}

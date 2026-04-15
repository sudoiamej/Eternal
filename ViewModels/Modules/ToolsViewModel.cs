using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class ToolsViewModel : ObservableObject
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
            MessageBox.Show(success ? "DNS Cache Flushed Successfully" : "Failed to Flush DNS");
        }

        [RelayCommand]
        private async Task ClearTemp()
        {
            long bytes = await _toolkitService.ClearTempFilesAsync();
            MessageBox.Show($"Cleared {(bytes / 1024 / 1024)} MB of temporary files.");
        }

        [RelayCommand]
        private async Task RebuildIcons()
        {
            bool success = await _toolkitService.RebuildIconCacheAsync();
            MessageBox.Show(success ? "Icon Cache Rebuild Triggered" : "Failed to rebuild cache. Ensure you have proper permissions.");
        }

        [RelayCommand]
        private async Task ResetNetwork()
        {
            bool success = await _toolkitService.ResetNetworkStackAsync();
            MessageBox.Show(success ? "Network Stack Reset. A system restart is highly recommended." : "Operation cancelled or failed.");
        }

        [RelayCommand]
        private async Task RepairSystemFiles()
        {
            bool success = await _toolkitService.RunSfcScanAsync();
            MessageBox.Show(success ? "System File Checker Completed." : "Operation cancelled or failed.");
        }

        [RelayCommand]
        private async Task RunDism()
        {
            bool success = await _toolkitService.RunDismRepairAsync();
            MessageBox.Show(success ? "DISM Restore Health Completed." : "Operation cancelled or failed.");
        }
    }
}

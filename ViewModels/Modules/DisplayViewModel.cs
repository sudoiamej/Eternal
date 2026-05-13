using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.Hardware;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class DisplayViewModel : BaseViewModel
    {
        private readonly IDisplayService _displayService;

        public ObservableCollection<MonitorInfo> Monitors { get; } = new();
        public ObservableCollection<DisplayAdapter> Adapters { get; } = new();

        [ObservableProperty] private MonitorInfo? _selectedMonitor;
        [ObservableProperty] private DisplayAdapter? _selectedAdapter;
        [ObservableProperty] private int _rollbackSeconds;
        [ObservableProperty] private bool _isApplyingChanges;

        public DisplayViewModel(IDisplayService displayService)
        {
            _displayService = displayService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        private async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var monitors = await _displayService.GetMonitorsAsync();
                Monitors.Clear();
                foreach (var m in monitors) Monitors.Add(m);

                var adapters = await _displayService.GetAdaptersAsync();
                Adapters.Clear();
                foreach (var a in adapters) Adapters.Add(a);

                if (SelectedMonitor == null && Monitors.Any())
                    SelectedMonitor = Monitors.First(m => m.IsPrimary) ?? Monitors.First();
                
                if (SelectedAdapter == null && Adapters.Any())
                    SelectedAdapter = Adapters.First();
            }, "Syncing Display Topology...");
        }

        [RelayCommand]
        private void SelectMonitor(MonitorInfo monitor)
        {
            SelectedMonitor = monitor;
        }

        [RelayCommand]
        private async Task IdentifyMonitor(MonitorInfo monitor)
        {
            await _displayService.IdentifyMonitorAsync(monitor);
        }

        [RelayCommand]
        private async Task ApplySettings()
        {
            if (SelectedMonitor == null) return;
            
            // Logic for rollback timer would go here
            IsApplyingChanges = true;
            bool success = await _displayService.ApplyDisplaySettingsAsync(SelectedMonitor, 1920, 1080, 60);
            IsApplyingChanges = false;
        }
    }
}

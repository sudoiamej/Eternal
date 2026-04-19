using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public enum HistoryType { Cpu, Ram, Disk }

    public partial class PerformanceViewModel : ObservableObject, IDisposable
    {
        private readonly IPerformanceService _performanceService;

        [ObservableProperty] private float _currentCpu;
        [ObservableProperty] private float _currentRam;
        [ObservableProperty] private float _currentDisk;
        [ObservableProperty] private HistoryType _selectedHistory = HistoryType.Cpu;
        
        public ObservableCollection<float> CpuHistory { get; } = new ObservableCollection<float>();
        public ObservableCollection<float> RamHistory { get; } = new ObservableCollection<float>();
        public ObservableCollection<float> DiskHistory { get; } = new ObservableCollection<float>();

        public PerformanceViewModel(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
            LoadCommand = new AsyncRelayCommand(UpdateAsync);
            SelectHistoryCommand = new RelayCommand<HistoryType>(type => SelectedHistory = type);
            
            // Subscribe to global updates to stay in sync with the status bar
            _performanceService.Updated += OnPerformanceUpdated;
        }

        private void OnPerformanceUpdated(object? sender, PerformanceSnapshot snap)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() =>
            {
                CurrentCpu = snap.CpuUsage;
                CurrentRam = snap.RamUsage;
                CurrentDisk = snap.DiskUsage;

                UpdateHistory(CpuHistory, CurrentCpu);
                UpdateHistory(RamHistory, CurrentRam);
                UpdateHistory(DiskHistory, CurrentDisk);
            });
        }

        public void Dispose()
        {
            _performanceService.Updated -= OnPerformanceUpdated;
        }

        private void UpdateHistory(ObservableCollection<float> history, float value)
        {
            history.Add(value);
            if (history.Count > 60) history.RemoveAt(0);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand SelectHistoryCommand { get; }

        private async Task UpdateAsync()
        {
            var snapshot = await _performanceService.GetCurrentSnapshotAsync();
            CurrentCpu = snapshot.CpuUsage;
            CurrentRam = snapshot.RamUsage;
            CurrentDisk = snapshot.DiskUsage;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.Views.Helpers;

namespace Eternal.ViewModels.Modules
{
    public partial class ProcessIntelligenceViewModel : BaseViewModel
    {
        private readonly IProcessService _processService;
        private DispatcherTimer? _refreshTimer;

        public ObservableCollection<ProcessDetail> FlatProcesses { get; } = new();
        [ObservableProperty] private int _totalProcessCount;
        [ObservableProperty] private ProcessDetail? _selectedProcess;

        public ProcessIntelligenceViewModel(IProcessService processService)
        {
            _processService = processService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new AsyncRelayCommand<ProcessDetail>(ShowDetails);
            SelectProcessCommand = new RelayCommand<ProcessDetail>(SelectProcess);
            KillProcessCommand = new AsyncRelayCommand<ProcessDetail>(KillProcess);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand<ProcessDetail> ShowDetailsCommand { get; }
        public IRelayCommand<ProcessDetail> SelectProcessCommand { get; }
        public IAsyncRelayCommand<ProcessDetail> KillProcessCommand { get; }

        private void SelectProcess(ProcessDetail? process)
        {
            SelectedProcess = process;
        }

        public override void Activate()
        {
            if (_refreshTimer != null) return;
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
            _refreshTimer.Start();
            _ = LoadDataAsync();
        }

        public override void Deactivate()
        {
            _refreshTimer?.Stop();
            _refreshTimer = null;
            base.Deactivate(); // Triggers ReleaseMemory
        }

        public override void ReleaseMemory()
        {
            FlatProcesses.Clear();
            SelectedProcess = null;
        }

        public async Task LoadDataAsync()
        {
            // Perform collection on background thread
            var result = await Task.Run(async () => 
            {
                var raw = await _processService.GetRunningProcessesAsync();
                var sortedRaw = raw.OrderByDescending(d => d.CpuUsage).ThenByDescending(d => d.MemoryBytes).ToList();
                return new { RawCount = raw.Count, Data = sortedRaw };
            });

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
            {
                // Optimization: Use Dictionary for O(1) lookups during reconciliation
                var processMap = FlatProcesses.ToDictionary(p => p.PID);
                var currentPids = result.Data.Select(p => p.PID).ToHashSet();

                // 1. Remove dead processes
                for (int i = FlatProcesses.Count - 1; i >= 0; i--)
                {
                    if (!currentPids.Contains(FlatProcesses[i].PID))
                    {
                        FlatProcesses.RemoveAt(i);
                    }
                }

                // 2. Add or Update processes
                foreach (var pData in result.Data)
                {
                    if (!processMap.TryGetValue(pData.PID, out var existingProc))
                    {
                        FlatProcesses.Add(pData);
                    }
                    else
                    {
                        // Update properties for live heatmap/telemetry
                        existingProc.CpuUsage = pData.CpuUsage;
                        existingProc.MemoryBytes = pData.MemoryBytes;
                        existingProc.DiskBytesPerSec = pData.DiskBytesPerSec;
                        existingProc.NetworkBytesPerSec = pData.NetworkBytesPerSec;
                        existingProc.Status = pData.Status;
                        existingProc.Impact = pData.Impact;
                    }
                }

                TotalProcessCount = result.RawCount;
            });
        }
        private async Task KillProcess(ProcessDetail? process)
        {
            if (process == null) return;
            var confirm = System.Windows.MessageBox.Show($"Terminate {process.Name} (PID: {process.PID})?", "Security Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                bool success = await _processService.KillProcessAsync(process.PID);
                if (success) await LoadDataAsync();
            }
        }

        private async Task ShowDetails(ProcessDetail? process)
        {
            if (process == null) return;
            
            var properties = new List<PropertyItem>
            {
                new PropertyItem("Process ID", process.PID.ToString()),
                new PropertyItem("Session ID", process.SessionId.ToString()),
                new PropertyItem("CPU Usage", process.CpuUsage.ToString("F1") + "%"),
                new PropertyItem("Working Set", process.MemoryUsage),
                new PropertyItem("Disk I/O", process.DiskUsage),
                new PropertyItem("Network I/O", process.NetworkUsage),
                new PropertyItem("Status", process.Status),
                new PropertyItem("File Path", process.Path)
            };

            var detailWin = new DetailWindow(process.Name, "PROCESS PROPERTIES", properties);
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }
    }
}

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

        public ObservableCollection<CategoryGroup> Categories { get; } = new();
        [ObservableProperty] private int _totalProcessCount;
        [ObservableProperty] private ProcessDetail? _selectedProcess;

        public ProcessIntelligenceViewModel(IProcessService processService)
        {
            _processService = processService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new AsyncRelayCommand<ProcessDetail>(ShowDetails);
            SelectProcessCommand = new RelayCommand<ProcessDetail>(SelectProcess);
            KillProcessCommand = new AsyncRelayCommand<ProcessDetail>(KillProcess);
            
            StartPolling();
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand<ProcessDetail> ShowDetailsCommand { get; }
        public IRelayCommand<ProcessDetail> SelectProcessCommand { get; }
        public IAsyncRelayCommand<ProcessDetail> KillProcessCommand { get; }

        private void SelectProcess(ProcessDetail? process)
        {
            SelectedProcess = process;
        }

        private void StartPolling()
        {
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
            _refreshTimer.Start();
        }

        public async Task LoadDataAsync()
        {
            var rawProcesses = await _processService.GetRunningProcessesAsync();
            
            // 2-Tier Grouping: Category -> Application Name -> PIDs
            var groupedData = rawProcesses
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key)
                .Select(cg => new {
                    Category = cg.Key,
                    Groups = cg.GroupBy(p => p.Name)
                               .Select(ag => new { Name = ag.Key, Procs = ag.ToList() })
                               .ToList()
                }).ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                // 1. Reconcile Categories
                foreach (var catData in groupedData)
                {
                    var existingCat = Categories.FirstOrDefault(c => c.Category == catData.Category);
                    if (existingCat == null)
                    {
                        var newCat = new CategoryGroup(catData.Category, Enumerable.Empty<ProcessGroup>());
                        Categories.Add(newCat);
                        existingCat = newCat;
                    }

                    // 2. Reconcile Groups within Category
                    var currentGroups = existingCat.Groups.ToList();
                    var newGroupNames = catData.Groups.Select(g => g.Name).ToHashSet();

                    // Remove dead groups
                    foreach (var oldG in currentGroups.Where(g => !newGroupNames.Contains(g.Name)))
                        existingCat.Groups.Remove(oldG);

                    // Add/Update groups
                    foreach (var gData in catData.Groups)
                    {
                        var existingGroup = existingCat.Groups.FirstOrDefault(g => g.Name == gData.Name);
                        if (existingGroup == null)
                        {
                            existingGroup = new ProcessGroup(gData.Name, Enumerable.Empty<ProcessDetail>(), catData.Category);
                            existingCat.Groups.Add(existingGroup);
                        }

                        // 3. Reconcile Processes within Group
                        var currentProcs = existingGroup.Processes.ToList();
                        var newPids = gData.Procs.Select(p => p.PID).ToHashSet();

                        // Remove dead processes
                        foreach (var oldP in currentProcs.Where(p => !newPids.Contains(p.PID)))
                            existingGroup.Processes.Remove(oldP);

                        // Add or Update processes
                        foreach (var pData in gData.Procs)
                        {
                            var existingProc = existingGroup.Processes.FirstOrDefault(p => p.PID == pData.PID);
                            if (existingProc == null)
                            {
                                existingGroup.Processes.Add(pData);
                            }
                            else
                            {
                                // Update properties for live heatmap
                                existingProc.CpuUsage = pData.CpuUsage;
                                existingProc.MemoryBytes = pData.MemoryBytes;
                                existingProc.DiskBytesPerSec = pData.DiskBytesPerSec;
                                existingProc.NetworkBytesPerSec = pData.NetworkBytesPerSec;
                                existingProc.Status = pData.Status;
                                existingProc.Impact = pData.Impact;
                            }
                        }
                    }
                }

                TotalProcessCount = rawProcesses.Count;
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

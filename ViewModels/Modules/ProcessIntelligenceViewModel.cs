using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.Views.Helpers;

namespace Eternal.ViewModels.Modules
{
    public partial class ProcessIntelligenceViewModel : BaseViewModel
    {
        private readonly IProcessService _processService;

        [ObservableProperty] private List<ProcessGroup> _processGroups = new();
        [ObservableProperty] private int _totalProcessCount;
        [ObservableProperty] private ProcessDetail? _selectedProcess;

        public ProcessIntelligenceViewModel(IProcessService processService)
        {
            _processService = processService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new AsyncRelayCommand<ProcessDetail>(ShowDetails);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand<ProcessDetail> ShowDetailsCommand { get; }

        public async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var rawProcesses = await _processService.GetRunningProcessesAsync();
                TotalProcessCount = rawProcesses.Count;

                ProcessGroups = rawProcesses
                    .GroupBy(p => p.Name)
                    .Select(g => new ProcessGroup(g.Key, g.ToList()))
                    .OrderByDescending(g => g.TotalMemory)
                    .ToList();
            }, "Analyzing Process Impact...");
        }

        private async Task ShowDetails(ProcessDetail? process)
        {
            if (process == null) return;
            
            var properties = new List<PropertyItem>
            {
                new PropertyItem("Process ID", process.PID.ToString()),
                new PropertyItem("Session ID", process.SessionId.ToString()),
                new PropertyItem("CPU Usage", process.CpuUsage.ToString("F1") + "%"),
                new PropertyItem("Working Set", process.MemoryUsage.ToString()),
                new PropertyItem("Status", process.Status),
                new PropertyItem("File Path", process.Path)
            };

            var detailWin = new DetailWindow(process.Name, "PROCESS PROPERTIES", properties);
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }
    }
}

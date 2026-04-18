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
    public partial class ProcessIntelligenceViewModel : ObservableObject
    {
        private readonly IProcessService _processService;

        [ObservableProperty] private List<ProcessGroup> _processGroups = new();
        [ObservableProperty] private int _totalProcessCount;
        [ObservableProperty] private bool _isLoading;

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
            if (IsLoading) return;
            IsLoading = true;
            try {
                var rawProcesses = await _processService.GetRunningProcessesAsync();
                TotalProcessCount = rawProcesses.Count;

                ProcessGroups = rawProcesses
                    .GroupBy(p => p.Name)
                    .Select(g => new ProcessGroup(g.Key, g.ToList()))
                    .OrderByDescending(g => g.TotalMemory)
                    .ToList();
            } 
            catch { ProcessGroups = new List<ProcessGroup>(); }
            finally { IsLoading = false; }
        }

        private async Task ShowDetails(ProcessDetail? process)
        {
            if (process == null) return;

            // Format memory
            string memory = "Unknown";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double doubleBytes = process.MemoryBytes;
            int unitIndex = 0;
            while (doubleBytes >= 1024 && unitIndex < units.Length - 1)
            {
                doubleBytes /= 1024;
                unitIndex++;
            }
            memory = $"{doubleBytes:F1} {units[unitIndex]}";

            var properties = new List<PropertyItem>
            {
                new PropertyItem("Process ID (PID)", process.Id.ToString()),
                new PropertyItem("Process Name", process.Name),
                new PropertyItem("CPU Usage", $"{process.CpuUsage:F1}%"),
                new PropertyItem("Memory Usage", memory),
                new PropertyItem("Path", process.Path),
                new PropertyItem("Signed", process.IsSigned ? "Yes" : "No"),
                new PropertyItem("System Impact", process.Impact)
            };

            try
            {
                var extendedInfo = await _processService.GetExtendedProcessInfoAsync(process);
                
                // Add Heuristics
                properties.Add(new PropertyItem("--- HEURISTIC ANALYSIS ---", "------------------------"));
                foreach (var reason in extendedInfo.HeuristicReasons)
                {
                    properties.Add(new PropertyItem("Anomaly Detected", reason));
                }

                // Add Static Imports
                properties.Add(new PropertyItem("--- STATIC IMPORTS (PE) ---", "------------------------"));
                if (extendedInfo.StaticImports.Count > 0)
                {
                    foreach (var imp in extendedInfo.StaticImports.Take(15)) // Limit display
                        properties.Add(new PropertyItem("Library Import", imp));
                    
                    if (extendedInfo.StaticImports.Count > 15)
                        properties.Add(new PropertyItem("...", $"and {extendedInfo.StaticImports.Count - 15} more..."));
                }
                else properties.Add(new PropertyItem("Imports", "None detected or analysis failed."));

                // Add Loaded Modules
                properties.Add(new PropertyItem("--- LOADED MODULES ---", "------------------------"));
                foreach (var mod in extendedInfo.LoadedModules.Take(20))
                {
                    properties.Add(new PropertyItem("Module", mod));
                }
                if (extendedInfo.LoadedModules.Count > 20)
                    properties.Add(new PropertyItem("...", $"and {extendedInfo.LoadedModules.Count - 20} more..."));
            }
            catch (Exception ex)
            {
                properties.Add(new PropertyItem("Extended Analysis", $"Error: {ex.Message}"));
            }

            var detailWin = new DetailWindow(process.Name, "PROCESS PROPERTIES", properties);
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }

        [RelayCommand]
        private async Task TerminateProcess(ProcessDetail process)
        {
            if (process == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to terminate '{process.Name}' (PID: {process.Id})?\n\nThis may cause system instability if it is a critical process.", 
                                         "HCI Safety Enforcement", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool success = await _processService.KillProcessAsync(process.Id);
                if (success)
                {
                    await LoadDataAsync();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to terminate process. Access denied or process already closed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
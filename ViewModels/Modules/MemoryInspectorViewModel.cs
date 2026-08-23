using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class MemoryInspectorViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<ProcessItemModel> _processes = new();
        [ObservableProperty] private ProcessItemModel? _selectedProcess;
        [ObservableProperty] private ObservableCollection<ProcessModuleModel> _modules = new();
        [ObservableProperty] private ObservableCollection<ProcessModuleModel> _filteredModules = new();
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusText = "Select a process to inspect loaded DLL modules & working set memory.";
        [ObservableProperty] private string _inspectorSummary = "No Process Selected";

        public MemoryInspectorViewModel()
        {
            _ = LoadProcessesAsync();
        }

        partial void OnSelectedProcessChanged(ProcessItemModel? value)
        {
            if (value != null)
            {
                _ = InspectProcessMemoryAsync(value);
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public async Task LoadProcessesAsync()
        {
            StatusText = "Enumerating active running processes...";
            await Task.Run(() =>
            {
                try
                {
                    var procs = Process.GetProcesses()
                        .OrderBy(p => p.ProcessName)
                        .Select(p =>
                        {
                            long workingSet = 0;
                            try { workingSet = p.WorkingSet64; } catch { }
                            return new ProcessItemModel
                            {
                                Pid = p.Id,
                                Name = p.ProcessName,
                                WorkingSetFormatted = FormatBytes(workingSet)
                            };
                        }).ToList();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Processes = new ObservableCollection<ProcessItemModel>(procs);
                        StatusText = $"Found {Processes.Count} process(es)";
                    });
                }
                catch (Exception ex)
                {
                    StatusText = $"Process Scan Error: {ex.Message}";
                }
            });
        }

        [RelayCommand]
        public async Task InspectProcessMemoryAsync(ProcessItemModel procItem)
        {
            StatusText = $"Inspecting DLL modules for PID {procItem.Pid} ({procItem.Name})...";
            InspectorSummary = $"{procItem.Name} (PID: {procItem.Pid}) — Working Set: {procItem.WorkingSetFormatted}";

            await Task.Run(() =>
            {
                var moduleList = new List<ProcessModuleModel>();
                try
                {
                    using var proc = Process.GetProcessById(procItem.Pid);
                    foreach (ProcessModule mod in proc.Modules)
                    {
                        try
                        {
                            moduleList.Add(new ProcessModuleModel
                            {
                                ModuleName = mod.ModuleName,
                                FilePath = mod.FileName,
                                BaseAddress = $"0x{mod.BaseAddress.ToInt64():X}",
                                MemorySizeFormatted = FormatBytes(mod.ModuleMemorySize),
                                Version = mod.FileVersionInfo.FileVersion ?? "N/A"
                            });
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Access Denied / Insufficient Privileges: {ex.Message}";
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Modules = new ObservableCollection<ProcessModuleModel>(moduleList);
                    ApplyFilter();
                    StatusText = $"Loaded {Modules.Count} module(s) for {procItem.Name}";
                });
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredModules = new ObservableCollection<ProcessModuleModel>(Modules);
            }
            else
            {
                var q = SearchQuery.ToLower();
                var filtered = Modules.Where(m => m.ModuleName.ToLower().Contains(q) || m.FilePath.ToLower().Contains(q)).ToList();
                FilteredModules = new ObservableCollection<ProcessModuleModel>(filtered);
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}

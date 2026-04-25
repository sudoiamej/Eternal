using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Eternal.ViewModels.Modules
{
    public record CommandItem(string Name, string Description, string Icon, string ViewName, string Category);

    public partial class CommandPaletteViewModel : ObservableObject
    {
        private List<CommandItem> _allCommands = new List<CommandItem>();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isOpen;
        
        public ObservableCollection<CommandItem> FilteredCommands { get; } = new ObservableCollection<CommandItem>();
        [ObservableProperty] private CommandItem? _selectedCommand;

        public CommandPaletteViewModel()
        {
            // Note: We don't initialize commands in the constructor anymore 
            // to avoid circular dependency with MainViewModel during DI resolution.
        }

        private void EnsureCommandsInitialized()
        {
            if (_allCommands.Count > 0) return;

            var mainVm = App.ServiceProvider.GetRequiredService<MainViewModel>();
            _allCommands.Clear();
            
            // 1. Navigation Modules
            AddFromNav(mainVm.SystemItems, "Module");
            AddFromNav(mainVm.TelemetryItems, "Module");
            AddFromNav(mainVm.MonitoringItems, "Module");
            AddFromNav(mainVm.SupportItems, "Module");
            
            // 2. Intelligence & Diagnostics (Deep Features)
            _allCommands.Add(new CommandItem("PC Scanner", "Run full system intelligence scan", "Search", "PcScanner", "Intelligence"));
            _allCommands.Add(new CommandItem("Sentinel Privacy", "Windows telemetry and permission audit", "EyeSlash", "Privacy", "Security"));
            _allCommands.Add(new CommandItem("Battery Lab", "Advanced ACPI battery diagnostics", "Bolt", "Battery", "Hardware"));
            _allCommands.Add(new CommandItem("File Forensics", "Cryptographic integrity and signature check", "FileTextOutline", "Forensics", "Security"));
            _allCommands.Add(new CommandItem("WinSAT Rating", "Benchmark Windows Experience Index", "Trophy", "PcRating", "Performance"));
            _allCommands.Add(new CommandItem("Registry Intelligence", "Deep audit of registry health", "List", "Registry", "Intelligence"));
            _allCommands.Add(new CommandItem("Eternal Doctor", "Automated system repair center", "Medkit", "Repair", "Repair"));
            _allCommands.Add(new CommandItem("Time Machine", "Analyze system state drift", "ClockOutline", "Snapshots", "Forensics"));
            _allCommands.Add(new CommandItem("Stress Hub", "CPU stability and thermal lab", "Flash", "StressTest", "Performance"));
            
            // 3. System Actions
            _allCommands.Add(new CommandItem("Clear Temp Files", "Purge Windows temporary file cache", "TrashOutline", "Dashboard", "Maintenance"));
            _allCommands.Add(new CommandItem("Purge RAM", "Release standby memory list", "Leaf", "Dashboard", "Maintenance"));
            _allCommands.Add(new CommandItem("Flush DNS", "Clear the DNS resolver cache", "Refresh", "Network", "Network"));
            _allCommands.Add(new CommandItem("Reset Network", "Reset network stack to defaults", "Wifi", "Network", "Network"));
            _allCommands.Add(new CommandItem("Check for Updates", "Manually check for Eternal updates", "Refresh", "Settings", "System"));
            _allCommands.Add(new CommandItem("System Restore", "Create a system restore point", "History", "Tuning", "Safety"));
            _allCommands.Add(new CommandItem("Uptime Audit", "Check system boot and live duration", "ClockOutline", "Dashboard", "System"));
        }

        private void AddFromNav(IEnumerable<NavigationItem> items, string category)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                _allCommands.Add(new CommandItem(item.Name, $"Navigate to {item.Name} module", item.Icon, item.ViewName, category));
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            EnsureCommandsInitialized();
            FilteredCommands.Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var cmd in _allCommands.Take(15)) FilteredCommands.Add(cmd);
                return;
            }

            var results = _allCommands
                .Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase) || 
                            c.Description.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                            c.Category.Contains(value, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name.StartsWith(value, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(c => c.Category.StartsWith(value, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .Take(20);

            foreach (var res in results) FilteredCommands.Add(res);
            SelectedCommand = FilteredCommands.FirstOrDefault();
        }

        [RelayCommand]
        public void ExecuteSelected()
        {
            if (SelectedCommand == null) return;

            var mainVm = App.ServiceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateCommand.Execute(SelectedCommand.ViewName);
            Close();
        }

        [RelayCommand]
        public void Open()
        {
            IsOpen = true;
            SearchText = "";
            OnSearchTextChanged("");
        }

        [RelayCommand]
        public void Close()
        {
            IsOpen = false;
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class CommandPaletteViewModel : BaseViewModel
    {
        [ObservableProperty] private bool _isOpen;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private CommandItem? _selectedCommand;
        
        public ObservableCollection<CommandItem> FilteredCommands { get; } = new();
        private readonly List<CommandItem> _allCommands = new();

        public CommandPaletteViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _allCommands.Clear();
            
            // Core
            _allCommands.Add(new CommandItem("Dashboard", "Overview of system health and telemetry", "Dashboard", "Dashboard", "System"));
            _allCommands.Add(new CommandItem("Settings", "Configure application behavior and appearance", "Gear", "Settings", "System"));
            
            // Intelligence
            _allCommands.Add(new CommandItem("PC Scanner", "Run full system intelligence scan", "Search", "PcScanner", "Intelligence"));
            _allCommands.Add(new CommandItem("Sentinel Privacy", "Windows telemetry and permission audit", "EyeSlash", "Privacy", "Security"));
            _allCommands.Add(new CommandItem("Battery Lab", "Advanced ACPI battery diagnostics", "Bolt", "Battery", "Hardware"));
            _allCommands.Add(new CommandItem("WinSAT Rating", "Benchmark Windows Experience Index", "Trophy", "PcRating", "Performance"));
            
            // Hardware
            _allCommands.Add(new CommandItem("Hardware", "Detailed hardware and sensor telemetry", "Microchip", "Hardware", "Hardware"));
            _allCommands.Add(new CommandItem("Stress Test", "CPU prime-based stability verification", "Flash", "StressTest", "Hardware"));
            _allCommands.Add(new CommandItem("Thermal", "Real-time temperature and fan monitoring", "ThermometerThreeQuarters", "Thermal", "Hardware"));
            _allCommands.Add(new CommandItem("Components", "Interactive hardware component diagnostics", "Laptop", "Components", "Hardware"));
            _allCommands.Add(new CommandItem("BIOS / UEFI", "Firmware and secure boot information", "InfoCircle", "Bios", "Hardware"));
            
            // System
            _allCommands.Add(new CommandItem("Eternal Doctor", "Automated system repair and diagnostics", "Stethoscope", "Repair", "System"));
            _allCommands.Add(new CommandItem("Registry", "Direct Windows Registry hive management", "Book", "Registry", "System"));
            _allCommands.Add(new CommandItem("Tools", "Power-user system maintenance toolkit", "Wrench", "Tools", "System"));
            _allCommands.Add(new CommandItem("Boot Records", "BCD and startup configuration audit", "List", "Boot", "System"));
            _allCommands.Add(new CommandItem("Storage", "Disk management and partition analysis", "HddOutline", "Storage", "System"));
            
            // Monitoring
            _allCommands.Add(new CommandItem("Processes", "Process intelligence and security analysis", "Tasks", "Processes", "Monitoring"));
            _allCommands.Add(new CommandItem("Performance", "Live CPU, RAM and Disk IO tracking", "LineChart", "Performance", "Monitoring"));
            _allCommands.Add(new CommandItem("Services", "System services and background task control", "Server", "Services", "Monitoring"));
            _allCommands.Add(new CommandItem("User Accounts", "Local user and group administration", "Users", "Users", "Monitoring"));
            _allCommands.Add(new CommandItem("Network", "Active connections and adapter telemetry", "Globe", "Network", "Monitoring"));
            _allCommands.Add(new CommandItem("Security", "Defender status and firewall validation", "Shield", "Security", "Monitoring"));
            _allCommands.Add(new CommandItem("Drivers", "Kernel driver and signature verification", "ListAlt", "Drivers", "Monitoring"));
            _allCommands.Add(new CommandItem("Environment", "Environment variable and path management", "Code", "Environment", "Monitoring"));
            
            // Support
            _allCommands.Add(new CommandItem("Eternal Console", "Integrated multi-tab diagnostic console", "Terminal", "Console", "Support"));
            _allCommands.Add(new CommandItem("Time Machine", "System restore and backup management", "ClockOutline", "Snapshots", "Support"));
            _allCommands.Add(new CommandItem("DISM Imaging", "Windows image servicing and repair", "Archive", "DismImaging", "Support"));
            _allCommands.Add(new CommandItem("Windows Update", "System update and patch management", "Refresh", "WindowsUpdate", "Support"));
            _allCommands.Add(new CommandItem("System Logs", "Windows event log and trace analysis", "Bars", "Logs", "Support"));
            _allCommands.Add(new CommandItem("Help", "Getting started and user documentation", "QuestionCircle", "Help", "Support"));
            _allCommands.Add(new CommandItem("PE Mode", "Pre-installation environment layout", "Medkit", "PeMode", "Support"));

            // Developer
            _allCommands.Add(new CommandItem("Unsafe Mode", "Request developer-level diagnostic authorization", "Terminal", "UnsafeMode", "Developer"));
            _allCommands.Add(new CommandItem("Feature Toggles", "Enable or disable application modules", "ToggleOn", "FeatureToggles", "Developer"));
            _allCommands.Add(new CommandItem("DevFlags Lab", "Critical engine behavior and simulation flags", "Flag", "Flags", "Developer"));
            
            OnSearchTextChanged("");
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredCommands.Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var cmd in _allCommands) FilteredCommands.Add(cmd);
            }
            else
            {
                var terms = value.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var matches = _allCommands.Where(c => 
                {
                    string searchable = $"{c.Name} {c.Description} {c.Category}".ToLower();
                    return terms.All(t => searchable.Contains(t));
                })
                .OrderByDescending(c => c.Name.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.Name);

                foreach (var cmd in matches) FilteredCommands.Add(cmd);
            }
            
            SelectedCommand = FilteredCommands.FirstOrDefault();
        }

        [RelayCommand]
        public void ExecuteSelected(CommandItem? item = null)
        {
            var target = item ?? SelectedCommand;
            if (target == null) return;

            // Close first to avoid UI focus issues during navigation
            IsOpen = false;

            var mainVm = App.ServiceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateCommand.Execute(target.ViewName);
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

    public record CommandItem(string Name, string Description, string Icon, string ViewName, string Category);
}

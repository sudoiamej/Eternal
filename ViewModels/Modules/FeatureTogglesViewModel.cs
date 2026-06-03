using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class FeatureToggleItem : ObservableObject
    {
        public string Name { get; }
        public string ViewName { get; }
        public string Category { get; }
        public bool IsHardDisabled { get; }
        public string DisabledReason { get; }

        [ObservableProperty] private bool _isEnabled;

        public FeatureToggleItem(string name, string viewName, string category, bool isEnabled, bool isHardDisabled = false, string reason = "")
        {
            Name = name;
            ViewName = viewName;
            Category = category;
            IsEnabled = isHardDisabled ? false : isEnabled;
            IsHardDisabled = isHardDisabled;
            DisabledReason = reason;
        }
    }

    public partial class FeatureTogglesViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IToastService _toastService;

        public ObservableCollection<FeatureToggleItem> Features { get; } = new();

        public FeatureTogglesViewModel(ISettingsService settingsService, IToastService toastService)
        {
            _settingsService = settingsService;
            _toastService = toastService;
            LoadFeatures();
        }

        private void LoadFeatures()
        {
            var disabled = _settingsService.Current.DisabledFeatures;
            Features.Clear();

            // System
            AddFeature("Dashboard", "Dashboard", "System", disabled);
            AddFeature("PC Scanner", "PcScanner", "System", disabled);
            AddFeature("Eternal Doctor", "Repair", "System", disabled);
            AddFeature("Registry", "Registry", "System", disabled);
            AddFeature("Reports", "Reports", "System", disabled);
            AddFeature("Tools", "Tools", "System", disabled);
            AddFeature("Guardian Tuning", "Tuning", "System", disabled);
            AddFeature("UI Toggle Button", "UiToggle", "System", disabled);

            // Telemetry
            AddFeature("Hardware", "Hardware", "Telemetry", disabled);
            AddFeature("Battery Lab", "Battery", "Telemetry", disabled);
            AddFeature("Stress Test", "StressTest", "Telemetry", disabled);
            AddFeature("PC Rating", "PcRating", "Telemetry", disabled);
            AddFeature("Thermal", "Thermal", "Telemetry", disabled);
            AddFeature("Components", "Components", "Telemetry", disabled);
            AddFeature("BIOS / UEFI", "Bios", "Telemetry", disabled);
            AddFeature("Boot Records", "Boot", "Telemetry", disabled);
            AddFeature("Storage", "Storage", "Telemetry", disabled);

            // Monitoring
            AddFeature("Processes", "Processes", "Monitoring", disabled);
            AddFeature("Performance", "Performance", "Monitoring", disabled);
            AddFeature("Sentinel Privacy", "Privacy", "Monitoring", disabled);
            AddFeature("Services", "Services", "Monitoring", disabled);
            AddFeature("User Accounts", "Users", "Monitoring", disabled);
            AddFeature("Network", "Network", "Monitoring", disabled);
            AddFeature("Security", "Security", "Monitoring", disabled);
            AddFeature("Drivers", "Drivers", "Monitoring", disabled);
            AddFeature("Environment", "Environment", "Monitoring", disabled);

            // Support
            AddFeature("Eternal Console", "Console", "Support", disabled);
            AddFeature("Time Machine", "Snapshots", "Support", disabled);
            AddFeature("DISM Imaging", "DismImaging", "Support", disabled);
            AddFeature("Windows Update", "WindowsUpdate", "Support", disabled);
            AddFeature("Settings", "Settings", "Support", disabled);
            AddFeature("System Logs", "Logs", "Support", disabled);
            AddFeature("PE Mode", "PeMode", "Support", disabled);

            // Permanently Disabled by Developer
            AddDisabledFeature("Neural Advisor", "Advisor", "Intelligence", "Disabled by Developer");
            AddFeature("File Forensics", "Forensics", "Security", disabled);
        }

        private void AddFeature(string name, string viewName, string category, List<string> disabled)
        {
            Features.Add(new FeatureToggleItem(name, viewName, category, !disabled.Contains(viewName)));
        }

        private void AddDisabledFeature(string name, string viewName, string category, string reason)
        {
            Features.Add(new FeatureToggleItem(name, viewName, category, false, true, reason));
        }

        [RelayCommand]
        private void SaveToggles()
        {
            var disabled = Features.Where(f => !f.IsEnabled && !f.IsHardDisabled).Select(f => f.ViewName).ToList();
            
            // Note: We don't save hard-disabled ones to the config list because they are hard-disabled anyway,
            // but we could if we wanted the logic to be more persistent.
            
            _settingsService.Current.DisabledFeatures = disabled;
            _settingsService.Save();
            
            _toastService.ShowSuccess("Feature configuration saved. Restart or re-navigate to apply.");
        }

        [RelayCommand]
        private void ResetToAll()
        {
            foreach (var f in Features)
            {
                if (!f.IsHardDisabled) f.IsEnabled = true;
            }
            SaveToggles();
        }
    }
}

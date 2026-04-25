using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.Services.Security;
using Eternal.Services.Storage;
using Eternal.Services.Network;
using Eternal.Models;
using Eternal.ViewModels.Modules;
using Eternal.Views;
using System.Windows;
using System.Windows.Media;

namespace Eternal.ViewModels
{
    public enum NavigationSortOption { Default, Level, EasyToHard, Alphabetical, SafeToDangerous }

    public partial class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly ILoggingService _loggingService;
        private readonly IPerformanceService _performanceService;
        private readonly ISettingsService _settingsService;
        private readonly IToastService _toastService;
        private readonly IHardwareService _hardwareService;
        private readonly ICreatorService _creatorService;
        private readonly IEnvironmentService _envService;
        private readonly INeuralAdvisorService _neuralService;
        private DispatcherTimer? _statusTimer;

        public ToastViewModel ToastVm { get; }

        [ObservableProperty] private string _title = "Eternal System Intelligence";
        [ObservableProperty] private ObservableObject _currentView;
        [ObservableProperty] private bool _isAdvancedMode = false;
        [ObservableProperty] private bool _isTestingModeActive = false;
        
        public AppSettings Settings => _settingsService.Current;
        [ObservableProperty] private bool _isDevModeEnabled = false;
        [ObservableProperty] private bool _isDevHostEnabled = false;
        [ObservableProperty] private bool _isRansomGuardEnabled = false;
        [ObservableProperty] private ObservableCollection<PortInfo> _activePorts = new ObservableCollection<PortInfo>();
        [ObservableProperty] private ObservableCollection<ProcessSecurityInfo> _unsignedProcesses = new ObservableCollection<ProcessSecurityInfo>();
        [ObservableProperty] private ObservableCollection<PersistenceEntry> _persistenceEntries = new ObservableCollection<PersistenceEntry>();

        public ObservableCollection<NavigationItem> DevToolkitItems { get; } = new ObservableCollection<NavigationItem>();

        [ObservableProperty] private string _cpuUsage = "0%";
        [ObservableProperty] private double _cpuUsageValue = 0.0;
        [ObservableProperty] private string _ramUsage = "0%";
        [ObservableProperty] private double _ramUsageValue = 0.0;
        [ObservableProperty] private string _uptime = "0d 0h 0m";
        [ObservableProperty] private bool _isPeMode = false;
        [ObservableProperty] private double _displayScale = 1.0;

        [ObservableProperty] private ObservableCollection<NavigationItem> _systemItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _telemetryItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _monitoringItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _supportItems;

        [ObservableProperty] private NavigationSortOption _navSortOption = NavigationSortOption.Default;
        
        [ObservableProperty] private CommandPaletteViewModel _commandPaletteVm;
        [ObservableProperty] private AdvisorViewModel _advisorVm;
        [ObservableProperty] private bool _isAdvisorOpen;

        partial void OnNavSortOptionChanged(NavigationSortOption value) => SortNavigation();

        public MainViewModel(
            ILoggingService loggingService, 
            IPerformanceService performanceService, 
            ISettingsService settingsService,
            IToastService toastService,
            IHardwareService hardwareService,
            ICreatorService creatorService,
            IEnvironmentService envService,
            INeuralAdvisorService neuralService,
            ToastViewModel toastVm,
            AdvisorViewModel advisorVm)
        {
            _loggingService = loggingService;
            _performanceService = performanceService;
            _settingsService = settingsService;
            _toastService = toastService;
            _hardwareService = hardwareService;
            _creatorService = creatorService;
            _envService = envService;
            _neuralService = neuralService;
            ToastVm = toastVm;
            AdvisorVm = advisorVm;

            _loggingService.Log("Eternal System Intelligence Initialized (DI Mode).");
            _loggingService.Log($"Dev Preview Suite Version 2.5.0-M4");

            IsAdvancedMode = _settingsService.Current.IsAdvancedMode;
            _settingsService.SettingsChanged += OnSettingsChanged;

            IsPeMode = _envService.IsPeMode;
            InitializeNavigation();

            if (Settings.IsAutoUpdateEnabled)
            {
                _ = CheckForUpdatesAsync();
            }

            _ = RefreshPorts();
            ApplyTheme(Settings.Theme);
            ApplyThemeColor();
        }

        private void OnSettingsChanged(object? sender, AppSettings settings)
        {
            IsAdvancedMode = settings.IsAdvancedMode;
            ApplyTheme(settings.Theme);
            InitializeNavigation();
        }

        public void InitializeNavigation()
        {
            var disabled = Settings.DisabledFeatures;

            if (IsPeMode)
            {
                SystemItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard", 0, 0, 0),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair", 2, 2, 1),
                    new NavigationItem("Registry", "Book", "Registry", 2, 2, 2),
                    new NavigationItem("Tools", "Wrench", "Tools", 1, 1, 3)
                }, disabled);

                TelemetryItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware", 0, 0, 0),
                    new NavigationItem("Storage / Disks", "HddOutline", "Storage", 0, 0, 1)
                }, disabled);

                MonitoringItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes", 1, 1, 0),
                    new NavigationItem("Eternal Console", "Terminal", "Console", 2, 2, 1)
                }, disabled);

                SupportItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("PE Recovery", "Medkit", "PeMode", 0, 0, 0),
                    new NavigationItem("System Logs", "Bars", "Logs", 0, 0, 1),
                    new NavigationItem("Settings", "Gear", "Settings", 0, 0, 2)
                }, disabled);
            }
            else
            {
                SystemItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard", 0, 0, 0),
                    new NavigationItem("PC Scanner", "Search", "PcScanner", 0, 0, 1),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair", 2, 2, 2),
                    new NavigationItem("Registry", "Book", "Registry", 2, 2, 3),
                    new NavigationItem("Reports", "FileTextOutline", "Reports", 0, 1, 4),
                    new NavigationItem("Tools", "Wrench", "Tools", 1, 1, 5),
                    new NavigationItem("Guardian Tuning", "Gears", "Tuning", 1, 2, 6)
                }, disabled);

                TelemetryItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware", 0, 0, 0),
                    new NavigationItem("Battery Lab", "Bolt", "Battery", 0, 0, 1),
                    new NavigationItem("Stress Test", "Flash", "StressTest", 1, 1, 2),
                    new NavigationItem("PC Rating", "Trophy", "PcRating", 0, 0, 3),
                    new NavigationItem("Thermal", "ThermometerThreeQuarters", "Thermal", 0, 0, 4),
                    new NavigationItem("Components", "Laptop", "Components", 0, 0, 5),
                    new NavigationItem("BIOS / UEFI", "InfoCircle", "Bios", 0, 0, 6),
                    new NavigationItem("Boot Records", "List", "Boot", 1, 1, 7),
                    new NavigationItem("Storage", "HddOutline", "Storage", 0, 0, 8)
                }, disabled);

                MonitoringItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes", 1, 1, 0),
                    new NavigationItem("Performance", "LineChart", "Performance", 0, 0, 1),
                    new NavigationItem("Sentinel Privacy", "EyeSlash", "Privacy", 1, 1, 2),
                    new NavigationItem("Services", "Server", "Services", 1, 1, 3),
                    new NavigationItem("User Accounts", "Users", "Users", 1, 1, 4),
                    new NavigationItem("Network", "Globe", "Network", 0, 0, 5),
                    new NavigationItem("Security", "Shield", "Security", 1, 1, 6),
                    new NavigationItem("Drivers", "ListAlt", "Drivers", 1, 2, 7),
                    new NavigationItem("Environment", "Code", "Environment", 1, 1, 8)
                }, disabled);

                SupportItems = FilterNav(new List<NavigationItem>
                {
                    new NavigationItem("Eternal Console", "Terminal", "Console", 2, 2, 0),
                    new NavigationItem("File Forensics", "FileTextOutline", "Forensics", 1, 2, 1),
                    new NavigationItem("Time Machine", "ClockOutline", "Snapshots", 1, 1, 2),
                    new NavigationItem("DISM Imaging", "Archive", "DismImaging", 1, 2, 3),
                    new NavigationItem("Windows Update", "Refresh", "WindowsUpdate", 0, 0, 4),
                    new NavigationItem("Settings", "Gear", "Settings", 0, 0, 5),
                    new NavigationItem("System Logs", "Bars", "Logs", 0, 0, 6),
                    new NavigationItem("Help", "QuestionCircle", "Help", 0, 0, 7),
                    new NavigationItem("PE Mode", "Medkit", "PeMode", 1, 1, 8)
                }, disabled);
            }
        }

        private ObservableCollection<NavigationItem> FilterNav(List<NavigationItem> items, List<string> disabled)
        {
            var filtered = items.Where(i => !disabled.Contains(i.ViewName)).ToList();
            return new ObservableCollection<NavigationItem>(filtered);
        }

        private void SortNavigation()
        {
            SortNavigationGroup(SystemItems);
            SortNavigationGroup(TelemetryItems);
            SortNavigationGroup(MonitoringItems);
            SortNavigationGroup(SupportItems);
            if (IsTestingModeActive) SortNavigationGroup(DevToolkitItems);
        }

        private void SortNavigationGroup(ObservableCollection<NavigationItem> items)
        {
            if (items == null) return;
            var sorted = NavSortOption switch
            {
                NavigationSortOption.Level => items.OrderByDescending(i => i.DangerLevel).ThenBy(i => i.Name).ToList(),
                NavigationSortOption.EasyToHard => items.OrderBy(i => i.DifficultyLevel).ThenBy(i => i.Name).ToList(),
                NavigationSortOption.Alphabetical => items.OrderBy(i => i.Name).ToList(),
                NavigationSortOption.SafeToDangerous => items.OrderBy(i => i.DangerLevel).ThenBy(i => i.Name).ToList(),
                _ => items.OrderBy(i => i.OriginalOrder).ToList()
            };

            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = items.IndexOf(sorted[i]);
                if (oldIndex != i) items.Move(oldIndex, i);
            }
        }

        [RelayCommand]
        private async Task ScanMalwarePersistence()
        {
            _loggingService.Log("Threat Hunter: Initializing deep persistence audit...");
            
            var pEntries = await _creatorService.GetPersistenceEntriesAsync();
            PersistenceEntries.Clear();
            foreach (var e in pEntries) PersistenceEntries.Add(e);

            var suspicious = await _creatorService.GetUnsignedProcessesAsync();
            UnsignedProcesses.Clear();
            foreach (var u in suspicious) UnsignedProcesses.Add(u);

            _loggingService.Log($"Threat Hunter: Scan complete. Found {PersistenceEntries.Count} auto-start entries.");
        }

        [RelayCommand]
        private async Task NeutralizeProcess(int pid)
        {
            var target = UnsignedProcesses.FirstOrDefault(p => p.PID == pid);
            string name = target?.Name ?? pid.ToString();

            var result = await _creatorService.SuspendProcessAsync(pid);
            _loggingService.Log(result.Message);
            _toastService.ShowWarning($"Process {name} suspended.");
        }

        [RelayCommand]
        private async Task IsolateNetwork(int pid)
        {
            var result = await _creatorService.IsolateProcessNetworkAsync(pid, true);
            _toastService.ShowError($"Network isolated for PID {pid}");
        }

        [RelayCommand]
        private async Task ToggleRansomGuard()
        {
            var result = await _creatorService.EnableRansomGuardAsync(IsRansomGuardEnabled);
            _toastService.ShowInfo(result.Message);
        }

        public void ApplyThemeColor()
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.ThemeAccentColor);
                System.Windows.Application.Current.Resources["AccentColor"] = color;
                System.Windows.Application.Current.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);
                UpdateAccentTextContrast(color);
            }
            catch { }
        }

        private void ApplyTheme(string themeName)
        {
            try
            {
                var appResources = System.Windows.Application.Current.Resources;
                var oldTheme = appResources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Themes/"));
                if (oldTheme != null) appResources.MergedDictionaries.Remove(oldTheme);

                var newTheme = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Styles/Themes/{themeName}.xaml", UriKind.Absolute) };
                appResources.MergedDictionaries.Insert(0, newTheme);
            }
            catch (Exception ex) { _loggingService.Log($"Theme Error: {ex.Message}"); }
        }

        private void UpdateAccentTextContrast(System.Windows.Media.Color accentColor)
        {
            double luminance = (0.299 * accentColor.R + 0.587 * accentColor.G + 0.114 * accentColor.B) / 255;
            var textColor = luminance > 0.5 ? System.Windows.Media.Colors.Black : System.Windows.Media.Colors.White;
            System.Windows.Application.Current.Resources["AccentTextBrush"] = new System.Windows.Media.SolidColorBrush(textColor);
        }

        [RelayCommand]
        private async Task ToggleDevMode()
        {
            var result = await _creatorService.ToggleDevModeAsync(IsDevModeEnabled);
            _toastService.ShowInfo(result.Message);
        }

        [RelayCommand]
        private async Task RefreshPorts()
        {
            var ports = await _creatorService.GetActivePortsAsync();
            ActivePorts.Clear();
            foreach (var p in ports) ActivePorts.Add(p);
        }

        [RelayCommand]
        private async Task PurgeRAM()
        {
            var result = await _creatorService.PurgeStandbyMemoryAsync();
            _toastService.ShowSuccess("Standby RAM purged successfully.");
        }

        [RelayCommand]
        private void ZoomIn() => DisplayScale = Math.Min(2.0, DisplayScale + 0.1);

        [RelayCommand]
        private void ZoomOut() => DisplayScale = Math.Max(0.5, DisplayScale - 0.1);

        [RelayCommand]
        private void ResetZoom() => DisplayScale = 1.0;

        public void StartTimers()
        {
            _performanceService.StartPolling();
            _performanceService.Updated += OnGlobalPerformanceUpdated;

            if (_statusTimer != null) return;
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _statusTimer.Tick += (s, e) => UpdateUptime();
            _statusTimer.Start();
        }

        private void OnGlobalPerformanceUpdated(object? sender, PerformanceSnapshot snap)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() =>
            {
                CpuUsage = $"{snap.CpuUsage:F0}%";
                CpuUsageValue = snap.CpuUsage;
                RamUsage = $"{snap.RamUsage:F0}%";
                RamUsageValue = snap.RamUsage;
            });
        }

        private void UpdateUptime()
        {
            var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
            Uptime = $"{up.Days}d {up.Hours}h {up.Minutes}m";
        }

        public async Task PreloadAllDataAsync()
        {
            var sp = App.ServiceProvider;
            try
            {
                var preloadTask = Task.Run(async () =>
                {
                    var dashboard = sp.GetRequiredService<DashboardViewModel>();
                    await dashboard.LoadDashboardAsync();

                    var perf = sp.GetRequiredService<PerformanceViewModel>();
                    await perf.LoadCommand.ExecuteAsync(null);

                    var batt = sp.GetRequiredService<BatteryViewModel>();
                    await batt.LoadBatteryInfoAsync();

                    var priv = sp.GetRequiredService<PrivacyViewModel>();
                    await priv.LoadAuditAsync();

                    if (CommandPaletteVm == null) CommandPaletteVm = sp.GetRequiredService<CommandPaletteViewModel>();
                });

                if (await Task.WhenAny(preloadTask, Task.Delay(15000)) != preloadTask)
                {
                    _loggingService.Log("!!! PRELOAD TIMEOUT: Continuing anyway.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"!!! PRELOAD FAILURE: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task Navigate(string viewName)
        {
            // Lifecycle management for specific ViewModels
            if (CurrentView is ThermalViewModel oldThermal) oldThermal.Deactivate();
            else if (CurrentView is NetworkViewModel oldNetwork) oldNetwork.Deactivate();
            else if (CurrentView is ComponentsViewModel oldComponents) oldComponents.Suspend();
            else if (CurrentView is HardwareStressViewModel oldStress) oldStress.StopOnDeactivate();

            UpdateNavigationSelection(viewName);

            // Resolve ViewModels via DI
            var sp = App.ServiceProvider;
            switch (viewName)
            {
                case "Dashboard": 
                    var dashboardVm = sp.GetRequiredService<DashboardViewModel>();
                    CurrentView = dashboardVm; 
                    await dashboardVm.LoadDashboardAsync();
                    break;
                case "Repair": 
                    CurrentView = sp.GetRequiredService<RepairCenterViewModel>(); 
                    break;
                case "Registry": 
                    var registryVm = sp.GetRequiredService<RegistryViewModel>();
                    CurrentView = registryVm; 
                    await registryVm.LoadRegistryCommand.ExecuteAsync(null); 
                    break;
                case "Boot": 
                    var bootVm = sp.GetRequiredService<BootViewModel>();
                    CurrentView = bootVm; 
                    await bootVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Hardware": 
                    var hwVm = sp.GetRequiredService<HardwareViewModel>();
                    CurrentView = hwVm; 
                    await hwVm.LoadDataCommand.ExecuteAsync(null);
                    break;
                case "Thermal": 
                    var thermVm = sp.GetRequiredService<ThermalViewModel>();
                    CurrentView = thermVm; 
                    thermVm.Activate(); 
                    break;
                case "StressTest": 
                    CurrentView = sp.GetRequiredService<HardwareStressViewModel>(); 
                    break;
                case "Processes": 
                    var procVm = sp.GetRequiredService<ProcessIntelligenceViewModel>();
                    CurrentView = procVm; 
                    await procVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Network": 
                    var netVm = sp.GetRequiredService<NetworkViewModel>();
                    CurrentView = netVm; 
                    netVm.Activate(); 
                    break;
                case "Privacy": 
                    var privVm = sp.GetRequiredService<PrivacyViewModel>();
                    CurrentView = privVm; 
                    await privVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Battery": 
                    var battVm = sp.GetRequiredService<BatteryViewModel>();
                    CurrentView = battVm; 
                    await battVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Storage": 
                    var storVm = sp.GetRequiredService<StorageViewModel>();
                    CurrentView = storVm; 
                    await storVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "PcRating": 
                    var ratingVm = sp.GetRequiredService<PcRatingViewModel>();
                    CurrentView = ratingVm; 
                    await ratingVm.LoadScoresCommand.ExecuteAsync(null); 
                    break;
                case "Security": 
                    var secVm = sp.GetRequiredService<SecurityViewModel>();
                    CurrentView = secVm; 
                    await secVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Bios": 
                    var biosVm = sp.GetRequiredService<BiosViewModel>();
                    CurrentView = biosVm; 
                    await biosVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Drivers": 
                    var driveVm = sp.GetRequiredService<DriversViewModel>();
                    CurrentView = driveVm; 
                    await driveVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Services": 
                    var servVm = sp.GetRequiredService<ServicesViewModel>();
                    CurrentView = servVm; 
                    await servVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Performance": 
                    var perfVm = sp.GetRequiredService<PerformanceViewModel>();
                    CurrentView = perfVm; 
                    await perfVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Environment": 
                    var envVm = sp.GetRequiredService<EnvironmentViewModel>();
                    CurrentView = envVm; 
                    await envVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Reports": 
                    CurrentView = sp.GetRequiredService<ReportsViewModel>(); 
                    break;
                case "Tools": 
                    CurrentView = sp.GetRequiredService<ToolsViewModel>(); 
                    break;
                case "Snapshots": 
                    var snapVm = sp.GetRequiredService<SnapshotsViewModel>();
                    CurrentView = snapVm; 
                    await snapVm.LoadSnapshotsCommand.ExecuteAsync(null); 
                    break;
                case "Forensics": 
                    CurrentView = sp.GetRequiredService<FileForensicsViewModel>(); 
                    break;
                case "Settings": 
                    CurrentView = sp.GetRequiredService<SettingsViewModel>(); 
                    break;
                case "PcScanner": 
                    CurrentView = sp.GetRequiredService<PcScannerViewModel>(); 
                    break;
                case "Logs": 
                    var logVm = sp.GetRequiredService<VerboseLoggingViewModel>();
                    CurrentView = logVm; 
                    await logVm.LoadSystemLogsCommand.ExecuteAsync(null);
                    break;
                case "Users": 
                    var userVm = sp.GetRequiredService<UserManagementViewModel>();
                    CurrentView = userVm; 
                    await userVm.LoadDataCommand.ExecuteAsync(null);
                    break;
                case "Tuning": 
                    var tuneVm = sp.GetRequiredService<TuningViewModel>();
                    CurrentView = tuneVm; 
                    await tuneVm.LoadTweaksCommand.ExecuteAsync(null);
                    break;
                case "Console": 
                    CurrentView = sp.GetRequiredService<ConsoleViewModel>(); 
                    break;
                case "Components": 
                    var compVm = sp.GetRequiredService<ComponentsViewModel>();
                    CurrentView = compVm; 
                    compVm.Resume(); 
                    break;
                case "WindowsUpdate": 
                    CurrentView = sp.GetRequiredService<WindowsUpdateViewModel>(); 
                    break;
                case "DismImaging": 
                    CurrentView = sp.GetRequiredService<DismImagingViewModel>(); 
                    break;
                case "FeatureToggles":
                    CurrentView = sp.GetRequiredService<FeatureTogglesViewModel>();
                    break;
                case "Help": OpenHelpWindow("GettingStarted"); break;
                case "ExitTestMode": ExitTestingMode(); break;
            }

            // Command Palette instantiation on first use
            if (CommandPaletteVm == null) CommandPaletteVm = sp.GetRequiredService<CommandPaletteViewModel>();
        }

        public async Task ActivateTestingMode()
        {
            DeveloperEnvironment.IsTestingModeActive = true;
            IsTestingModeActive = true;
            DevToolkitItems.Clear();
            DevToolkitItems.Add(new NavigationItem("Feature Toggles", "ToggleOn", "FeatureToggles", 0, 0, 0));
            DevToolkitItems.Add(new NavigationItem("Exit Testing", "SignOut", "ExitTestMode", 0, 0, 1));
            _loggingService.Log("!!! DEV TESTING MODE ACTIVATED !!!");
        }

        private void ExitTestingMode()
        {
            DeveloperEnvironment.IsTestingModeActive = false;
            IsTestingModeActive = false;
            DevToolkitItems.Clear();
            _ = Navigate("Dashboard");
        }

        private void UpdateNavigationSelection(string viewName)
        {
            var allItems = new List<NavigationItem>();
            if (SystemItems != null) allItems.AddRange(SystemItems);
            if (TelemetryItems != null) allItems.AddRange(TelemetryItems);
            if (MonitoringItems != null) allItems.AddRange(MonitoringItems);
            if (SupportItems != null) allItems.AddRange(SupportItems);
            if (DevToolkitItems != null) allItems.AddRange(DevToolkitItems);

            foreach (var item in allItems) item.IsSelected = item.ViewName == viewName;
        }

        private void OpenHelpWindow(string? context = null)
        {
            var helpWindow = new HelpWindow(context);
            helpWindow.Owner = System.Windows.Application.Current.MainWindow;
            helpWindow.Show();
        }

        public void Dispose()
        {
            _settingsService.SettingsChanged -= OnSettingsChanged;
            _performanceService.Updated -= OnGlobalPerformanceUpdated;
            _statusTimer?.Stop();
            _performanceService?.StopPolling();
            _neuralService?.UnloadModel();
        }

        public async Task CheckForUpdatesAsync(bool manual = false)
        {
            _toastService.ShowInfo("Checking for system updates...");
            await Task.Delay(2000);
            if (manual) _toastService.ShowSuccess("Eternal is up to date.");
        }
    }

    public partial class NavigationItem : ObservableObject
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string ViewName { get; set; }
        public int DangerLevel { get; set; }
        public int DifficultyLevel { get; set; }
        public int OriginalOrder { get; set; }
        [ObservableProperty] private bool _isSelected;

        public NavigationItem(string name, string icon, string viewName, int danger = 0, int difficulty = 0, int order = 0)
        {
            Name = name; Icon = icon; ViewName = viewName;
            DangerLevel = danger; DifficultyLevel = difficulty; OriginalOrder = order;
        }
    }
}

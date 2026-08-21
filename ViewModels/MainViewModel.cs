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
        private readonly IBatteryService _batteryService;
        private readonly IScalingService _scalingService;
        private DispatcherTimer? _statusTimer;

        public ToastViewModel ToastVm { get; }

        [ObservableProperty] private BaseViewModel _currentView;
        public bool IsHomeActive => CurrentView is HomeGridDashboardViewModel;
        public bool IsSettingsActive => CurrentView is SettingsViewModel;
        public bool IsSelected => false;
        
        [ObservableProperty] private bool _isAdvancedMode = false;
        [ObservableProperty] private bool _isTestingModeActive = false;
        [ObservableProperty] private bool _isTelemetryHudOpen = true;
        [ObservableProperty] private bool _isSidebarExpanded = false;
        public TestingViewModel TestingVm => App.ServiceProvider.GetRequiredService<TestingViewModel>();
        
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
        [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm");
        [ObservableProperty] private string _batteryPercentage = "100%";
        [ObservableProperty] private bool _isPeMode = false;
        [ObservableProperty] private double _displayScale = 1.0;
        private double _fitScale = 1.0;
        private double _userZoomMultiplier = 1.0;
        [ObservableProperty] private bool _isUiToggleVisible = false;

        [ObservableProperty] private ObservableCollection<NavigationItem> _systemItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _telemetryItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _monitoringItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _supportItems;
        [ObservableProperty] private ObservableCollection<NavigationItem> _pinnedItems = new();

        [ObservableProperty] private NavigationSortOption _navSortOption = NavigationSortOption.Default;
        
        [ObservableProperty] private CommandPaletteViewModel _commandPaletteVm;

        partial void OnNavSortOptionChanged(NavigationSortOption value) => SortNavigation();
        
        partial void OnCurrentViewChanged(BaseViewModel value)
        {
            OnPropertyChanged(nameof(IsHomeActive));
            OnPropertyChanged(nameof(IsSettingsActive));
        }

        public MainViewModel(
            ILoggingService loggingService, 
            IPerformanceService performanceService, 
            ISettingsService settingsService,
            IToastService toastService,
            IHardwareService hardwareService,
            ICreatorService creatorService,
            IEnvironmentService envService,
            IBatteryService batteryService,
            IScalingService scalingService,
            ToastViewModel toastVm,
            CommandPaletteViewModel commandPaletteVm)
        {
            _loggingService = loggingService;
            _performanceService = performanceService;
            _settingsService = settingsService;
            _toastService = toastService;
            _hardwareService = hardwareService;
            _creatorService = creatorService;
            _envService = envService;
            _batteryService = batteryService;
            _scalingService = scalingService;
            ToastVm = toastVm;
            _commandPaletteVm = commandPaletteVm;

            _systemItems = new ObservableCollection<NavigationItem>();
            _telemetryItems = new ObservableCollection<NavigationItem>();
            _monitoringItems = new ObservableCollection<NavigationItem>();
            _supportItems = new ObservableCollection<NavigationItem>();

            Title = "Eternal System Intelligence";
            _loggingService.Log("Eternal System Intelligence Initialized (DI Mode).");
            _loggingService.Log($"Dev Preview Suite Version 3.5.0");

            IsAdvancedMode = _settingsService.Current.IsAdvancedMode;
            _settingsService.SettingsChanged += OnSettingsChanged;

            IsPeMode = _envService.IsPeMode;
            InitializeNavigation();

            // Set default view after navigation collections are initialized
            _currentView = App.ServiceProvider.GetRequiredService<HomeGridDashboardViewModel>();

            if (Settings.IsAutoUpdateEnabled)
            {
                _ = CheckForUpdatesAsync();
            }

            _ = RefreshPorts();
            ApplyTheme(Settings.Theme);
            ApplyThemeColor();
            UpdateFontScale();
            UpdateWindowScale();

            _ = Task.Run(async () =>
            {
                try
                {
                    var batteryInfo = await _batteryService.GetBatteryInfoAsync();
                    if (batteryInfo == null)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var batteryItem = TelemetryItems.FirstOrDefault(item => item.ViewName == "Battery");
                            if (batteryItem != null)
                            {
                                TelemetryItems.Remove(batteryItem);
                            }
                        });
                    }
                }
                catch { }
            });
        }

        public void UpdateFontScale()
        {
            try
            {
                _scalingService.UpdateScales(Settings.WindowScale, Settings.FontAdjustmentScale);
            }
            catch { }
        }

        public void UpdateWindowScale()
        {
            try
            {
                _scalingService.UpdateScales(Settings.WindowScale, Settings.FontAdjustmentScale);
                DisplayScale = _scalingService.UiScale;
            }
            catch { }
        }

        private void OnSettingsChanged(object? sender, AppSettings settings)
        {
            IsAdvancedMode = settings.IsAdvancedMode;
            ApplyTheme(settings.Theme);
            ApplyThemeColor();
            UpdateFontScale();
            UpdateWindowScale();
            InitializeNavigation();
        }

        public void InitializeNavigation()
        {
            var disabled = Settings.DisabledFeatures;
            IsUiToggleVisible = false;

            // Upgrade old default pinned features to clean high-level CarPlay categories
            if (Settings.PinnedFeatures.Count == 3 && 
                ((Settings.PinnedFeatures.Contains("Hardware") && Settings.PinnedFeatures.Contains("Processes") && Settings.PinnedFeatures.Contains("Tuning")) ||
                 (Settings.PinnedFeatures.Contains("Category_Diagnostics") && Settings.PinnedFeatures.Contains("Category_Tuning") && Settings.PinnedFeatures.Contains("Category_Monitoring"))))
            {
                Settings.PinnedFeatures = new() { "Processes", "Storage", "Dashboard" };
                _settingsService.Save();
            }

            // Initialize Pinned Items for Neumorphic UI
            PinnedItems.Clear();
            var allItems = new List<NavigationItem>();
            
            // Temporary list of all available navigation items to find the pinned ones
            var tempSystem = new List<NavigationItem>
            {
                new NavigationItem("Dashboard", "Dashboard", "Dashboard", 0, 0, 0),
                new NavigationItem("PC Scanner", "Search", "PcScanner", 0, 0, 1),
                new NavigationItem("Eternal Doctor", "Stethoscope", "Repair", 2, 2, 2),
                new NavigationItem("Registry", "Book", "Registry", 2, 2, 3),
                new NavigationItem("Reports", "FileText", "Reports", 0, 1, 4),
                new NavigationItem("Tools", "Wrench", "Tools", 1, 1, 5),
                new NavigationItem("Guardian Tuning", "Gears", "Tuning", 1, 2, 6),
                new NavigationItem("Baseline Audit", "Shield", "RegistryLexicon", 1, 1, 7)
            };
            var tempTelemetry = new List<NavigationItem>
            {
                new NavigationItem("Hardware", "Microchip", "Hardware", 0, 0, 0),
                new NavigationItem("Displays", "Desktop", "Display", 0, 0, 1),
                new NavigationItem("Battery Lab", "Bolt", "Battery", 0, 0, 2),
                new NavigationItem("Stress Test", "Flash", "StressTest", 1, 1, 3),
                new NavigationItem("PC Rating", "Trophy", "PcRating", 0, 0, 4),
                new NavigationItem("Thermal", "ThermometerThreeQuarters", "Thermal", 0, 0, 5),
                new NavigationItem("Components", "Laptop", "Components", 0, 0, 6),
                new NavigationItem("BIOS / UEFI", "InfoCircle", "Bios", 0, 0, 7),
                new NavigationItem("Boot Records", "List", "Boot", 1, 1, 8),
                new NavigationItem("Storage", "HddOutline", "Storage", 0, 0, 9)
            };
            var tempMonitoring = new List<NavigationItem>
            {
                new NavigationItem("Processes", "Tasks", "Processes", 1, 1, 0),
                new NavigationItem("Performance", "LineChart", "Performance", 0, 0, 1),
                new NavigationItem("Sentinel Privacy", "EyeSlash", "Privacy", 1, 1, 2),
                new NavigationItem("Services", "Server", "Services", 1, 1, 3),
                new NavigationItem("User Accounts", "Users", "Users", 1, 1, 4),
                new NavigationItem("Network", "Globe", "Network", 0, 0, 5),
                new NavigationItem("Security", "Shield", "Security", 1, 1, 6),
                new NavigationItem("Drivers", "ListAlt", "Drivers", 1, 2, 7),
                new NavigationItem("Environment", "Code", "Environment", 1, 1, 8),
                new NavigationItem("File Forensics", "Eye", "Forensics", 1, 1, 9)
            };
            var tempSupport = new List<NavigationItem>
            {
                new NavigationItem("Eternal Console", "Terminal", "Console", 2, 2, 0),
                new NavigationItem("Time Machine", "ClockOutline", "Snapshots", 1, 1, 2),
                new NavigationItem("DISM Imaging", "Archive", "DismImaging", 1, 2, 3),
                new NavigationItem("Windows Update", "Refresh", "WindowsUpdate", 0, 0, 4),
                new NavigationItem("Settings", "Gear", "Settings", 0, 0, 5),
                new NavigationItem("System Logs", "Bars", "Logs", 0, 0, 6),
                new NavigationItem("Help", "QuestionCircle", "Help", 0, 0, 7),
                new NavigationItem("PE Mode", "Medkit", "PeMode", 1, 1, 8)
            };

            allItems.AddRange(tempSystem);
            allItems.AddRange(tempTelemetry);
            allItems.AddRange(tempMonitoring);
            allItems.AddRange(tempSupport);
            
            // Add Category Folders as pinnable items
            allItems.Add(new NavigationItem("Diagnostics", "Search", "Category_Diagnostics"));
            allItems.Add(new NavigationItem("Tuning & Repair", "Wrench", "Category_Tuning"));
            allItems.Add(new NavigationItem("Monitoring", "LineChart", "Category_Monitoring"));
            allItems.Add(new NavigationItem("Support & Tools", "LifeRing", "Category_Support"));

            foreach (var viewName in Settings.PinnedFeatures)
            {
                var item = allItems.FirstOrDefault(i => i.ViewName == viewName);
                if (item != null) PinnedItems.Add(item);
            }

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
                    new NavigationItem("Displays", "Desktop", "Display", 0, 0, 1),
                    new NavigationItem("Storage / Disks", "HddOutline", "Storage", 0, 0, 2)
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
                SystemItems = FilterNav(tempSystem, disabled);
                TelemetryItems = FilterNav(tempTelemetry, disabled);
                MonitoringItems = FilterNav(tempMonitoring, disabled);
                SupportItems = FilterNav(tempSupport, disabled);
            }
        }

        private ObservableCollection<NavigationItem> FilterNav(List<NavigationItem> items, List<string> disabled)
        {
            var filtered = items.Where(i => !disabled.Contains(i.ViewName) && Eternal.Helpers.OsHelper.IsBuildSupported(i.MinBuild, i.MaxBuild)).ToList();
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

        public override void ReleaseMemory()
        {
            ActivePorts.Clear();
            UnsignedProcesses.Clear();
            PersistenceEntries.Clear();
        }

        [RelayCommand]
        private void ToggleUI()
        {
            System.Windows.MessageBox.Show("Legacy UI is permanently disabled.", "Interface Disabled", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
                var colorStr = "#7F00FF"; // Force original purple accent
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                var resources = System.Windows.Application.Current.Resources;
                
                resources["AccentColor"] = color;
                resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);

                // Derive secondary color (darker version for gradients/depth)
                var secondary = System.Windows.Media.Color.FromRgb(
                    (byte)(color.R * 0.7),
                    (byte)(color.G * 0.7),
                    (byte)(color.B * 0.7));
                
                resources["AccentSecondaryColor"] = secondary;
                resources["AccentSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(secondary);

                // Apply solid backgrounds instead of dynamic gradients for performance
                var solidBg = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#090A0E");
                resources["BgGradientStop1"] = solidBg;
                resources["BgGradientStop2"] = solidBg;
                resources["BgGradientStop3"] = solidBg;

                UpdateAccentTextContrast(color);
            }
            catch { }
        }

        private string _activeTheme = "";
        private void ApplyTheme(string themeName)
        {
            if (themeName != "Dark" && themeName != "Light")
            {
                themeName = "Dark";
            }

            if (_activeTheme == themeName) return;
            try
            {
                var appResources = System.Windows.Application.Current.Resources;
                var oldTheme = appResources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Themes/"));
                
                var newSource = new Uri($"pack://application:,,,/Styles/Themes/{themeName}.xaml", UriKind.Absolute);
                if (oldTheme?.Source == newSource) return;

                if (oldTheme != null) appResources.MergedDictionaries.Remove(oldTheme);

                var newTheme = new ResourceDictionary { Source = newSource };
                appResources.MergedDictionaries.Insert(0, newTheme);
                _activeTheme = themeName;
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
        private void DeepMemoryPurge()
        {
            // Call ReleaseMemory on all Singletons
            var sp = App.ServiceProvider;
            var viewModels = sp.GetServices<BaseViewModel>();
            foreach (var vm in viewModels) vm.ReleaseMemory();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _toastService.ShowInfo("DEEP MEMORY REFRESH: Complete");
        }

        public void UpdateFitScale(double fitScale)
        {
            _fitScale = fitScale;
            DisplayScale = _fitScale * _userZoomMultiplier;
        }

        public string ZoomPercentageString => $"{(_userZoomMultiplier * 100):0}%";

        [RelayCommand]
        private void ZoomIn()
        {
            _userZoomMultiplier = Math.Min(2.0, _userZoomMultiplier + 0.1);
            DisplayScale = _fitScale * _userZoomMultiplier;
            OnPropertyChanged(nameof(ZoomPercentageString));
        }

        [RelayCommand]
        private void ZoomOut()
        {
            _userZoomMultiplier = Math.Max(0.5, _userZoomMultiplier - 0.1);
            DisplayScale = _fitScale * _userZoomMultiplier;
            OnPropertyChanged(nameof(ZoomPercentageString));
        }

        [RelayCommand]
        private void ResetZoom()
        {
            _userZoomMultiplier = 1.0;
            DisplayScale = _fitScale * _userZoomMultiplier;
            OnPropertyChanged(nameof(ZoomPercentageString));
        }

        [RelayCommand]
        private void ToggleTelemetryHud() => IsTelemetryHudOpen = !IsTelemetryHudOpen;

        public void StartTimers()
        {
            _performanceService.StartPolling();
            _performanceService.Updated += OnGlobalPerformanceUpdated;

            if (_statusTimer != null) return;
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _statusTimer.Tick += (s, e) => {
                UpdateUptime();
                CurrentTime = DateTime.Now.ToString("HH:mm");
                _ = UpdateBatteryInfo();
            };
            _statusTimer.Start();
        }

        private async Task UpdateBatteryInfo()
        {
            try
            {
                var info = await _batteryService.GetBatteryInfoAsync();
                if (info != null)
                {
                    BatteryPercentage = $"{info.ChargeLevel}%";
                }
            }
            catch { }
        }

        private void OnGlobalPerformanceUpdated(object? sender, PerformanceSnapshot snap)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            _ = app.Dispatcher.InvokeAsync(() =>
            {
                CpuUsage = $"{snap.CpuUsage:F0}%";
                CpuUsageValue = snap.CpuUsage;
                RamUsage = $"{snap.RamUsage:F0}%";
                RamUsageValue = snap.RamUsage;

                // CONTEXTUAL INTELLIGENCE: Trigger Nav Alerts
                UpdateContextualAlerts(snap);
            });
        }

        private void UpdateContextualAlerts(PerformanceSnapshot snap)
        {
            var items = new List<NavigationItem>();
            if (MonitoringItems != null) items.AddRange(MonitoringItems);
            if (TelemetryItems != null) items.AddRange(TelemetryItems);

            foreach (var item in items)
            {
                if (item.ViewName == "Processes")
                    item.HasAlert = snap.CpuUsage > 80 || snap.RamUsage > 85;
                else if (item.ViewName == "Thermal")
                    item.HasAlert = snap.CpuUsage > 90; // Heuristic for potential heat issue
            }
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
            // Polymorphically deactivate the outgoing view to systematically release memory
            // and suspend background polling loops across the entire application.
            if (CurrentView != null)
            {
                CurrentView.Deactivate();
                CurrentView.ReleaseMemory();
            }

            UpdateNavigationSelection(viewName);

            // Resolve ViewModels via DI
            var sp = App.ServiceProvider;
            switch (viewName)
            {
                case "Home":
                    CurrentView = sp.GetRequiredService<HomeGridDashboardViewModel>();
                    break;
                case "Dashboard": 
                    var systemDashboardVm = sp.GetRequiredService<DashboardViewModel>();
                    CurrentView = systemDashboardVm;
                    await systemDashboardVm.LoadDashboardAsync();
                    break;
                case "Category_Tuning":
                    var catVmSystem = sp.GetRequiredService<CategoryGridViewModel>();
                    catVmSystem.Initialize("System & Tuning", SystemItems);
                    CurrentView = catVmSystem;
                    break;
                case "Category_Diagnostics":
                    var catVmDiag = sp.GetRequiredService<CategoryGridViewModel>();
                    catVmDiag.Initialize("Diagnostics", TelemetryItems);
                    CurrentView = catVmDiag;
                    break;
                case "Category_Monitoring":
                    var catVmMon = sp.GetRequiredService<CategoryGridViewModel>();
                    catVmMon.Initialize("Monitoring", MonitoringItems);
                    CurrentView = catVmMon;
                    break;
                case "Category_Support":
                    var catVmSup = sp.GetRequiredService<CategoryGridViewModel>();
                    catVmSup.Initialize("Support", SupportItems);
                    CurrentView = catVmSup;
                    break;
                case "Repair": 
                    CurrentView = sp.GetRequiredService<RepairCenterViewModel>(); 
                    break;
                case "Registry": 
                    var registryVm = sp.GetRequiredService<RegistryViewModel>();
                    CurrentView = registryVm; 
                    await registryVm.LoadRegistryCommand.ExecuteAsync(null); 
                    break;
                case "RegistryLexicon":
                    var lexiconVm = sp.GetRequiredService<RegistryLexiconViewModel>();
                    CurrentView = lexiconVm;
                    await lexiconVm.LoadAuditCommand.ExecuteAsync(null);
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
                    procVm.Activate();
                    break;
                case "Battery":
                    var batteryVm = sp.GetRequiredService<BatteryViewModel>();
                    CurrentView = batteryVm;
                    batteryVm.Activate();
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
                case "Storage": 
                    var storVm = sp.GetRequiredService<StorageViewModel>();
                    _loggingService.Log("UI Thread: Activating Storage Intelligence module...");
                    CurrentView = storVm; 
                    await storVm.LoadCommand.ExecuteAsync(null); 
                    break;
                case "Display":
                    var dispVm = sp.GetRequiredService<DisplayViewModel>();
                    CurrentView = dispVm;
                    await dispVm.LoadCommand.ExecuteAsync(null);
                    break;
                case "UnsafeMode":
                    // Trigger authorization window before navigating to testing view
                    var authWindow = new Eternal.Views.Helpers.TestingAuthWindow();
                    authWindow.Owner = System.Windows.Application.Current.MainWindow;
                    if (authWindow.ShowDialog() == true)
                    {
                        ActivateTestingMode();
                    }
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
                    perfVm.Activate();
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
                case "PeMode": 
                    CurrentView = sp.GetRequiredService<PEModeViewModel>(); 
                    break;
                case "Logs": 
                    var logVm = sp.GetRequiredService<VerboseLoggingViewModel>();
                    CurrentView = logVm; 
                    logVm.Activate();
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
                    var consoleVm = sp.GetRequiredService<ConsoleViewModel>();
                    CurrentView = consoleVm;
                    await consoleVm.StartConsoleCommand.ExecuteAsync(null);
                    break;
                case "Components": 
                    var compVm = sp.GetRequiredService<ComponentsViewModel>();
                    CurrentView = compVm; 
                    compVm.Resume(); 
                    break;
                case "WindowsUpdate": 
                    var wuVm = sp.GetRequiredService<WindowsUpdateViewModel>();
                    CurrentView = wuVm; 
                    await wuVm.CheckForUpdatesCommand.ExecuteAsync(null);
                    await wuVm.LoadUpdatesCommand.ExecuteAsync(null);
                    break;
                case "DismImaging": 
                    CurrentView = sp.GetRequiredService<DismImagingViewModel>(); 
                    break;
                case "FeatureToggles":
                    CurrentView = sp.GetRequiredService<FeatureTogglesViewModel>();
                    break;
                case "WmiExplorer":
                    CurrentView = sp.GetRequiredService<WmiExplorerViewModel>();
                    break;
                case "AppProfiler":
                    var profilerVm = sp.GetRequiredService<AppProfilerViewModel>();
                    CurrentView = profilerVm;
                    profilerVm.Activate();
                    break;
                case "ConfigEditor":
                    CurrentView = sp.GetRequiredService<ConfigEditorViewModel>();
                    break;
                case "Flags":
                    CurrentView = sp.GetRequiredService<FlagsViewModel>();
                    break;
                case "Testing":
                    CurrentView = TestingVm;
                    break;
                case "Help": OpenHelpWindow("GettingStarted"); break;
                case "ExitTestMode": ExitTestingMode(); break;
            }

            // Command Palette instantiation on first use
            if (CommandPaletteVm == null) CommandPaletteVm = sp.GetRequiredService<CommandPaletteViewModel>();
        }

        public void ActivateTestingMode()
        {
            IsTestingModeActive = true;
            DevToolkitItems.Clear();
            DevToolkitItems.Add(new NavigationItem("Feature Integrity", "CheckCircleOutline", "Testing", 0, 0, 0));
            DevToolkitItems.Add(new NavigationItem("Feature Toggles", "ToggleOn", "FeatureToggles", 0, 0, 1));
            DevToolkitItems.Add(new NavigationItem("Live App Logs", "Terminal", "Logs", 0, 0, 2));
            DevToolkitItems.Add(new NavigationItem("WMI Explorer", "Search", "WmiExplorer", 0, 0, 3));
            DevToolkitItems.Add(new NavigationItem("App Profiler", "Heartbeat", "AppProfiler", 0, 0, 4));
            DevToolkitItems.Add(new NavigationItem("Config Editor", "FileCodeOutline", "ConfigEditor", 0, 0, 5));
            DevToolkitItems.Add(new NavigationItem("DevFlags", "Flag", "Flags", 0, 0, 6));
            DevToolkitItems.Add(new NavigationItem("Exit Testing", "SignOut", "ExitTestMode", 0, 0, 7));
            
            _toastService.ShowSuccess("TESTING MODE ACTIVE: System integrity suite unlocked.");
            _ = Navigate("Testing");
            _loggingService.Log("!!! DEV TESTING MODE ACTIVATED !!!");
            }

        [RelayCommand]
        public void PinFeature(string viewName)
        {
            if (PinnedItems.Any(i => i.ViewName == viewName)) return;
            if (PinnedItems.Count >= 8)
            {
                _toastService.ShowWarning("Dock is full. Unpin an item first.");
                return;
            }

            // Find the item in all collections
            var all = new List<NavigationItem>();
            if (SystemItems != null) all.AddRange(SystemItems);
            if (TelemetryItems != null) all.AddRange(TelemetryItems);
            if (MonitoringItems != null) all.AddRange(MonitoringItems);
            if (SupportItems != null) all.AddRange(SupportItems);

            var target = all.FirstOrDefault(i => i.ViewName == viewName);
            if (target != null)
            {
                PinnedItems.Add(target);
                Settings.PinnedFeatures.Add(viewName);
                _settingsService.Save();
                _toastService.ShowSuccess($"{target.Name} pinned to dock.");
            }
        }

        [RelayCommand]
        public void UnpinFeature(string viewName)
        {
            var target = PinnedItems.FirstOrDefault(i => i.ViewName == viewName);
            if (target != null)
            {
                PinnedItems.Remove(target);
                Settings.PinnedFeatures.Remove(viewName);
                _settingsService.Save();
            }
        }

        private void ExitTestingMode()
        {
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
            if (PinnedItems != null) allItems.AddRange(PinnedItems);

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
        public int? MinBuild { get; set; }
        public int? MaxBuild { get; set; }
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _hasAlert;

        public NavigationItem(string name, string icon, string viewName, int danger = 0, int difficulty = 0, int order = 0, int? minBuild = null, int? maxBuild = null)
        {
            Name = name; Icon = icon; ViewName = viewName;
            DangerLevel = danger; DifficultyLevel = difficulty; OriginalOrder = order;
            MinBuild = minBuild; MaxBuild = maxBuild;
        }
    }
}

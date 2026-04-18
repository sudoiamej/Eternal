using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.Services.Security;
using Eternal.Services.Storage;
using Eternal.Services.Network;
using Eternal.Models;
using Eternal.ViewModels.Modules;
using System.Windows;
using System.Windows.Media;

namespace Eternal.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly ILoggingService _loggingService;
        private readonly ILibreHardwareService _libreService;
        private readonly IHardwareService _hardwareService;
        private readonly IBiosService _biosService;
        private readonly ISecurityService _securityService;
        private readonly IIntelligenceService _intelligenceService;
        private readonly IPerformanceService _performanceService;
        private readonly IToolkitService _toolkitService;
        private readonly IDriversService _driversService;
        private readonly IServicesService _servicesService;
        private readonly IStorageService _storageService;
        private readonly INetworkService _networkService;
        private readonly IProcessService _processService;
        private readonly IThermalService _thermalService;
        private readonly IEnvironmentService _envService;
        private readonly ISettingsService _settingsService;
        private readonly ITuningService _tuningService;
        private readonly IConsoleService _consoleService;
        private readonly IBootService _bootService;
        private readonly ICreatorService _creatorService;
        private readonly IRegistryService _registryService;
        private readonly IUserGroupService _userGroupService;
        private DispatcherTimer _statusTimer;

        // Persistent ViewModels
        private DashboardViewModel _dashboardVm;
        private RepairCenterViewModel _repairVm;
        private RegistryViewModel _registryVm;
        private TuningViewModel _tuningVm;
        private ConsoleViewModel _consoleVm;
        private BootViewModel _bootVm;
        private HardwareViewModel _hardwareVm;
        private BiosViewModel _biosVm;
        private SecurityViewModel _securityVm;
        private PerformanceViewModel _performanceVm;
        private DriversViewModel _driversVm;
        private ServicesViewModel _servicesVm;
        private StorageViewModel _storageVm;
        private NetworkViewModel _networkVm;
        private ReportsViewModel _reportsVm;
        private ToolsViewModel _toolsVm;
        private SettingsViewModel _settingsVm;
        private ProcessIntelligenceViewModel _processVm;
        private ThermalViewModel _thermalVm;
        private EnvironmentViewModel _envVm;
        private VerboseLoggingViewModel _logsVm;
        private UserManagementViewModel _userVm;
        private ComponentsViewModel _componentsVm;

        [ObservableProperty] private string _title = "Eternal System Intelligence";
        [ObservableProperty] private ObservableObject _currentView;
        [ObservableProperty] private bool _isAdvancedMode = false;
        
        public AppSettings Settings => _settingsService.Current;
        [ObservableProperty] private bool _isDevModeEnabled = false;
        [ObservableProperty] private bool _isDevHostEnabled = false;
        [ObservableProperty] private bool _isRansomGuardEnabled = false;
        [ObservableProperty] private ObservableCollection<PortInfo> _activePorts = new ObservableCollection<PortInfo>();
        [ObservableProperty] private ObservableCollection<ProcessSecurityInfo> _unsignedProcesses = new ObservableCollection<ProcessSecurityInfo>();
        [ObservableProperty] private ObservableCollection<PersistenceEntry> _persistenceEntries = new ObservableCollection<PersistenceEntry>();

        [RelayCommand]
        private async Task ScanMalwarePersistence()
        {
            var pEntries = await _creatorService.GetPersistenceEntriesAsync();
            PersistenceEntries.Clear();
            foreach (var e in pEntries) PersistenceEntries.Add(e);

            var unsigned = await _creatorService.GetUnsignedProcessesAsync();
            UnsignedProcesses.Clear();
            foreach (var u in unsigned) UnsignedProcesses.Add(u);
        }

        [RelayCommand]
        private async Task NeutralizeProcess(int pid)
        {
            var result = await _creatorService.SuspendProcessAsync(pid);
            _loggingService.Log(result.Message);
            ScanMalwarePersistenceCommand.Execute(null);
        }

        [RelayCommand]
        private async Task IsolateNetwork(int pid)
        {
            var result = await _creatorService.IsolateProcessNetworkAsync(pid, true);
            _loggingService.Log(result.Message);
            System.Windows.MessageBox.Show(result.Message, "Malware Hunter", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        [RelayCommand]
        private async Task ToggleRansomGuard()
        {
            var result = await _creatorService.EnableRansomGuardAsync(IsRansomGuardEnabled);
            _loggingService.Log(result.Message);
        }
        
        [ObservableProperty] private string _cpuUsage = "0%";
        [ObservableProperty] private string _ramUsage = "0%";
        [ObservableProperty] private string _uptime = "0d 0h 0m";
        [ObservableProperty] private bool _isPeMode = false;
        [ObservableProperty] private double _displayScale = 1.0;

        public ObservableCollection<NavigationItem> SystemItems { get; }
        public ObservableCollection<NavigationItem> TelemetryItems { get; }
        public ObservableCollection<NavigationItem> MonitoringItems { get; }
        public ObservableCollection<NavigationItem> SupportItems { get; }

        public MainViewModel()
        {
            // Lightweight service instantiation
            _settingsService = new SettingsService();
            _loggingService = new WindowsLoggingService(_settingsService);
            _libreService = new WindowsLibreHardwareService();
            _hardwareService = new WindowsHardwareService(_libreService);
            _biosService = new WindowsBiosService();
            _securityService = new WindowsSecurityService();
            _performanceService = new WindowsPerformanceService();
            _intelligenceService = new WindowsIntelligenceService(_performanceService, _securityService);
            _toolkitService = new WindowsToolkitService();
            _driversService = new WindowsDriversService();
            _servicesService = new WindowsServicesService();
            _storageService = new WindowsStorageService();
            _networkService = new WindowsNetworkService();
            _processService = new WindowsProcessService();
            _thermalService = new WindowsThermalService(_libreService);
            _envService = new WindowsEnvironmentService();
            _tuningService = new WindowsTuningService();
            _consoleService = new WindowsConsoleService();
            _bootService = new WindowsBootService();
            _creatorService = new WindowsCreatorService();
            _registryService = new WindowsRegistryService();
            _userGroupService = new WindowsUserGroupService();

            _loggingService.Log("Eternal System Intelligence Initialized.");
            _loggingService.Log($"Dev Preview Suite Version 2.5.0-M1");

            IsAdvancedMode = _settingsService.Current.IsAdvancedMode;
            _settingsService.SettingsChanged += (s, settings) => IsAdvancedMode = settings.IsAdvancedMode;

            DetectPeMode();
            InitializeViewModels();

            if (IsPeMode)
            {
                // PE Mode: Focused Recovery Environment
                SystemItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard"),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair"),
                    new NavigationItem("Registry", "Book", "Registry"),
                    new NavigationItem("Tools", "Wrench", "Tools")
                };

                TelemetryItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware"),
                    new NavigationItem("Storage / Disks", "HddOutline", "Storage"),
                    new NavigationItem("Network", "Globe", "Network"),
                    new NavigationItem("Boot Records", "List", "Boot")
                };

                MonitoringItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes")
                };

                SupportItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("System Logs", "Bars", "Logs"),
                    new NavigationItem("PE Mode Status", "Medkit", "PeMode")
                };
            }
            else
            {
                // Standard Mode: Full Suite
                SystemItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard"),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair"),
                    new NavigationItem("Registry", "Book", "Registry"),
                    new NavigationItem("Reports", "FileTextOutline", "Reports"),
                    new NavigationItem("Tools", "Wrench", "Tools"),
                    new NavigationItem("Guardian Tuning", "Gears", "Tuning")
                };

                TelemetryItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware"),
                    new NavigationItem("Thermal", "ThermometerThreeQuarters", "Thermal"),
                    new NavigationItem("Components", "Laptop", "Components"),
                    new NavigationItem("BIOS / UEFI", "InfoCircle", "Bios"),
                    new NavigationItem("Boot Records", "List", "Boot"),
                    new NavigationItem("Storage", "HddOutline", "Storage")
                };

                MonitoringItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes"),
                    new NavigationItem("Performance", "LineChart", "Performance"),
                    new NavigationItem("Services", "Server", "Services"),
                    new NavigationItem("User Accounts", "Users", "Users"),
                    new NavigationItem("Network", "Globe", "Network"),
                    new NavigationItem("Security", "Shield", "Security"),
                    new NavigationItem("Drivers", "ListAlt", "Drivers"),
                    new NavigationItem("Environment", "Code", "Environment")
                };

                SupportItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Eternal Console", "Terminal", "Console"),
                    new NavigationItem("Settings", "Gear", "Settings"),
                    new NavigationItem("System Logs", "Bars", "Logs"),
                    new NavigationItem("Help", "QuestionCircle", "Help"),
                    new NavigationItem("PE Mode", "Medkit", "PeMode")
                };
            }

            // Set initial view
            Navigate("Dashboard");
            RefreshPortsCommand.Execute(null);

            ApplyThemeColor();
        }

        public void ApplyThemeColor()
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.ThemeAccentColor);
                System.Windows.Application.Current.Resources["AccentColor"] = color;
                System.Windows.Application.Current.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);

                // Create a slightly lighter secondary color
                var secondary = System.Windows.Media.Color.FromArgb(color.A, 
                    (byte)Math.Min(255, color.R + 30), 
                    (byte)Math.Min(255, color.G + 30), 
                    (byte)Math.Min(255, color.B + 30));
                System.Windows.Application.Current.Resources["AccentSecondaryColor"] = secondary;
                System.Windows.Application.Current.Resources["AccentSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(secondary);
            }
            catch { }
        }

        [RelayCommand]
        private async Task ToggleDevMode()
        {
            var result = await _creatorService.ToggleDevModeAsync(IsDevModeEnabled);
            _loggingService.Log(result.Message);
            System.Windows.MessageBox.Show(result.Message, "Dev Preview Trick", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ApplySilenceProfile()
        {
            var result = await _creatorService.ApplyServiceProfileAsync("Absolute Silence");
            _loggingService.Log(result.Message);
            System.Windows.MessageBox.Show(result.Message, "Dev Preview Trick", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task RefreshPorts()
        {
            var ports = await _creatorService.GetActivePortsAsync();
            ActivePorts.Clear();
            foreach (var p in ports) ActivePorts.Add(p);
        }

        [RelayCommand]
        private async Task ForceKillHandle()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Select Locked File to Force Kill" };
            if (dialog.ShowDialog() == true)
            {
                var result = await _creatorService.IdentifyAndKillFileHandleAsync(dialog.FileName);
                _loggingService.Log(result.Message);
                System.Windows.MessageBox.Show(result.Message, "Dev Preview Trick", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private async Task ValidatePath()
        {
            var deadLinks = await _creatorService.ValidateEnvironmentPathAsync();
            if (deadLinks.Count == 0)
            {
                System.Windows.MessageBox.Show("All entries in PATH are valid.", "Variable Vault", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                string message = "Found dead links in PATH:\n" + string.Join("\n", deadLinks);
                System.Windows.MessageBox.Show(message, "Variable Vault Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task ToggleDevHost()
        {
            var result = await _creatorService.ToggleDevHostEntryAsync(IsDevHostEnabled);
            _loggingService.Log(result.Message);
            System.Windows.MessageBox.Show(result.Message, "Host Master", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task PurgeRAM()
        {
            var result = await _creatorService.PurgeStandbyMemoryAsync();
            _loggingService.Log(result.Message);
            System.Windows.MessageBox.Show(result.Message, "RAM Purge", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task CreateJunction()
        {
            // Simplified Junction creation for now
            System.Windows.MessageBox.Show("Please use the drag-and-drop 'Junction Studio' module (coming soon) or use mklink manually for now.", "Symlink Studio", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ZoomIn()
        {
            if (DisplayScale < 2.0) DisplayScale += 0.1;
        }

        [RelayCommand]
        private void ZoomOut()
        {
            if (DisplayScale > 0.5) DisplayScale -= 0.1;
        }

        [RelayCommand]
        private void ResetZoom()
        {
            DisplayScale = 1.0;
        }

        public void StartTimers()
        {
            _performanceService.StartPolling();
            _performanceService.Updated += (s, snap) => 
            {
                var app = System.Windows.Application.Current;
                if (app == null) return;

                app.Dispatcher.Invoke(() => 
                {
                    CpuUsage = $"{snap.CpuUsage:F0}%";
                    RamUsage = $"{snap.RamUsage:F0}%";
                });
            };

            if (_statusTimer != null) return;
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _statusTimer.Tick += (s, e) => UpdateUptime();
            _statusTimer.Start();
        }

        private void UpdateUptime()
        {
            var up = TimeSpan.FromMilliseconds(Environment.TickCount64);
            Uptime = $"{up.Days}d {up.Hours}h {up.Minutes}m";
        }

        private void DetectPeMode()
        {
            IsPeMode = Directory.Exists(@"X:\Windows\System32") || 
                       Process.GetCurrentProcess().MainModule?.FileName.StartsWith("X:", StringComparison.OrdinalIgnoreCase) == true;
        }

        private void InitializeViewModels()
        {
            _dashboardVm = new DashboardViewModel(_hardwareService, _biosService, _securityService, _intelligenceService, _toolkitService);
            _repairVm = new RepairCenterViewModel(_toolkitService, _servicesService);
            _registryVm = new RegistryViewModel(_registryService);
            _hardwareVm = new HardwareViewModel(_hardwareService);
            _biosVm = new BiosViewModel(_biosService);
            _securityVm = new SecurityViewModel(_securityService);
            _performanceVm = new PerformanceViewModel(_performanceService);
            _driversVm = new DriversViewModel(_driversService);
            _servicesVm = new ServicesViewModel(_servicesService);
            _storageVm = new StorageViewModel(_storageService);
            _networkVm = new NetworkViewModel(_hardwareService, _networkService);
            _reportsVm = new ReportsViewModel(_hardwareService);
            _toolsVm = new ToolsViewModel(_toolkitService);
            _settingsVm = new SettingsViewModel(this, _settingsService);
            _processVm = new ProcessIntelligenceViewModel(_processService);
            _thermalVm = new ThermalViewModel(_thermalService);
            _envVm = new EnvironmentViewModel(_envService);
            _logsVm = new VerboseLoggingViewModel(_loggingService);
            _tuningVm = new TuningViewModel(_tuningService);
            _consoleVm = new ConsoleViewModel(_consoleService);
            _bootVm = new BootViewModel(_bootService);
            _userVm = new UserManagementViewModel(_userGroupService);
            _componentsVm = new ComponentsViewModel();
        }

        public async Task PreloadAllDataAsync()
        {
            // Group 1: Critical Core Data (Parallel)
            var group1 = new List<Task>
            {
                _dashboardVm.LoadDashboardCommand.ExecuteAsync(null),
                _hardwareVm.LoadDataCommand.ExecuteAsync(null),
                _biosVm.LoadCommand.ExecuteAsync(null),
                _securityVm.LoadCommand.ExecuteAsync(null)
            };
            await Task.WhenAll(group1);

            // Group 2: Secondary System Data (Parallel)
            var group2 = new List<Task>
            {
                _driversVm.LoadCommand.ExecuteAsync(null),
                _servicesVm.LoadCommand.ExecuteAsync(null),
                _storageVm.LoadCommand.ExecuteAsync(null),
                _networkVm.LoadCommand.ExecuteAsync(null)
            };
            await Task.WhenAll(group2);

            // Group 3: Intelligence & Environment (Parallel)
            var group3 = new List<Task>
            {
                _processVm.LoadCommand.ExecuteAsync(null),
                _thermalVm.LoadCommand.ExecuteAsync(null),
                _envVm.LoadCommand.ExecuteAsync(null),
                _tuningVm.LoadTweaksCommand.ExecuteAsync(null),
                _bootVm.LoadCommand.ExecuteAsync(null)
            };
            await Task.WhenAll(group3);
        }

        [RelayCommand]
        private async Task Navigate(string viewName)
        {
            _loggingService.Log($"Navigating to: {viewName}");
            
            // Deactivate current view if applicable
            if (CurrentView is ThermalViewModel oldThermal) oldThermal.Deactivate();
            else if (CurrentView is NetworkViewModel oldNetwork) oldNetwork.Deactivate();
            else if (CurrentView is ComponentsViewModel oldComponents) oldComponents.Suspend();

            UpdateNavigationSelection(viewName);

            switch (viewName)
            {
                case "Dashboard": 
                    CurrentView = _dashboardVm; 
                    await _dashboardVm.LoadDashboardCommand.ExecuteAsync(null);
                    break;
                case "Repair":
                    CurrentView = _repairVm;
                    break;
                case "Registry":
                    CurrentView = _registryVm;
                    await _registryVm.LoadRegistryCommand.ExecuteAsync(null);
                    break;
                case "Boot":
                    CurrentView = _bootVm;
                    await _bootVm.LoadCommand.ExecuteAsync(null);
                    break;
                case "Hardware": CurrentView = _hardwareVm; break;
                case "Thermal": 
                    CurrentView = _thermalVm; 
                    _thermalVm.Activate();
                    break;
                case "Processes": 
                    CurrentView = _processVm; 
                    await _processVm.LoadCommand.ExecuteAsync(null);
                    break;
                case "Network": 
                    CurrentView = _networkVm; 
                    _networkVm.Activate();
                    break;
                case "Storage": CurrentView = _storageVm; break;
                case "Security": CurrentView = _securityVm; break;
                case "Bios": CurrentView = _biosVm; break;
                case "Drivers": CurrentView = _driversVm; break;
                case "Services": 
                    CurrentView = _servicesVm; 
                    await _servicesVm.LoadCommand.ExecuteAsync(null);
                    break;
                case "Performance": CurrentView = _performanceVm; break;
                case "Environment": 
                    CurrentView = _envVm; 
                    await _envVm.LoadCommand.ExecuteAsync(null);
                    break;
                case "Reports": CurrentView = _reportsVm; break;
                case "Tools": CurrentView = _toolsVm; break;
                case "Help": OpenHelpWindow(); break;
                case "PeMode": CurrentView = new PEModeViewModel(_hardwareService, IsPeMode); break;
                case "Settings": CurrentView = _settingsVm; break;
                case "Logs": 
                    CurrentView = _logsVm; 
                    _logsVm.RefreshLogs();
                    break;
                case "Users":
                    CurrentView = _userVm;
                    await _userVm.LoadDataCommand.ExecuteAsync(null);
                    break;
                case "Tuning": 
                    CurrentView = _tuningVm; 
                    await _tuningVm.LoadTweaksCommand.ExecuteAsync(null);
                    break;
                case "Console": 
                    CurrentView = _consoleVm; 
                    await _consoleVm.StartConsoleCommand.ExecuteAsync(null);
                    break;
                case "Components":
                    CurrentView = _componentsVm;
                    _componentsVm.Resume();
                    break;
            }
        }

        private void UpdateNavigationSelection(string viewName)
        {
            var allItems = new List<NavigationItem>();
            if (SystemItems != null) allItems.AddRange(SystemItems);
            if (TelemetryItems != null) allItems.AddRange(TelemetryItems);
            if (MonitoringItems != null) allItems.AddRange(MonitoringItems);
            if (SupportItems != null) allItems.AddRange(SupportItems);

            foreach (var item in allItems)
            {
                item.IsSelected = item.ViewName == viewName;
            }
        }

        private void OpenHelpWindow()
        {
            var helpWindow = new Eternal.Views.HelpWindow();
            helpWindow.Owner = System.Windows.Application.Current.MainWindow;
            helpWindow.Show();
        }

        public void Dispose()
        {
            _statusTimer?.Stop();
            _performanceService?.StopPolling();
            _consoleService?.Stop();
            
            if (_libreService is IDisposable d1) d1.Dispose();
            if (_thermalService is IDisposable d2) d2.Dispose();
        }
    }

    public partial class NavigationItem : ObservableObject
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string ViewName { get; set; }
        [ObservableProperty] private bool _isSelected;

        public NavigationItem(string name, string icon, string viewName)
        {
            Name = name; Icon = icon; ViewName = viewName;
        }
    }
}

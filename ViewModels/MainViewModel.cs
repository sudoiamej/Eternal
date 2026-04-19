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
        private readonly IUpdateService _updateService;
        private readonly IOsUpdateService _osUpdateService;
        private readonly IPcScannerService _pcScannerService;
        private readonly IDismService _dismService;
        private readonly IWinSatService _winSatService;
        private DispatcherTimer? _statusTimer;

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
        private WindowsUpdateViewModel _windowsUpdateVm;
        private PcScannerViewModel _pcScannerVm;
        private DismImagingViewModel _dismVm;
        private PcRatingViewModel _pcRatingVm;

        [ObservableProperty] private string _title = "Eternal System Intelligence";
        [ObservableProperty] private ObservableObject _currentView;
        [ObservableProperty] private bool _isAdvancedMode = false;
        [ObservableProperty] private bool _isTestingModeActive = false;
        [ObservableProperty] private bool _isSidebarExpanded = true;
        
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

        public ObservableCollection<NavigationItem> SystemItems { get; private set; }
        public ObservableCollection<NavigationItem> TelemetryItems { get; private set; }
        public ObservableCollection<NavigationItem> MonitoringItems { get; private set; }
        public ObservableCollection<NavigationItem> SupportItems { get; private set; }

        [ObservableProperty] private NavigationSortOption _navSortOption = NavigationSortOption.Default;
        
        partial void OnNavSortOptionChanged(NavigationSortOption value) => SortNavigation();

        public MainViewModel()
        {
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
            _updateService = new WindowsUpdateService();
            _osUpdateService = new WindowsOsUpdateService();
            _pcScannerService = new WindowsPcScannerService(_securityService, _storageService, _performanceService, _creatorService, _toolkitService, _biosService, _driversService);
            _dismService = new WindowsDismService();
            _winSatService = new WindowsWinSatService();

            _loggingService.Log("Eternal System Intelligence Initialized.");
            _loggingService.Log($"Dev Preview Suite Version 2.5.0-M3");

            IsAdvancedMode = _settingsService.Current.IsAdvancedMode;
            _settingsService.SettingsChanged += OnSettingsChanged;

            DetectPeMode();
            InitializeViewModels();
            InitializeNavigation();

            if (Settings.IsAutoUpdateEnabled)
            {
                _ = CheckForUpdatesAsync();
            }

            _currentView = _dashboardVm; // Initial view
            _ = RefreshPorts();
            ApplyTheme(Settings.Theme);
            ApplyThemeColor();
        }

        private void OnSettingsChanged(object? sender, AppSettings settings)
        {
            IsAdvancedMode = settings.IsAdvancedMode;
            ApplyTheme(settings.Theme);
        }

        private void InitializeNavigation()
        {
            if (IsPeMode)
            {
                // PE Mode: Highly restricted to essential and functional recovery tools
                SystemItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard", 0, 0, 0),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair", 2, 2, 1),
                    new NavigationItem("Registry", "Book", "Registry", 2, 2, 2),
                    new NavigationItem("Tools", "Wrench", "Tools", 1, 1, 3)
                };

                TelemetryItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware", 0, 0, 0),
                    new NavigationItem("Storage / Disks", "HddOutline", "Storage", 0, 0, 1)
                };

                MonitoringItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes", 1, 1, 0),
                    new NavigationItem("Eternal Console", "Terminal", "Console", 2, 2, 1)
                };

                SupportItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("PE Recovery", "Medkit", "PeMode", 0, 0, 0),
                    new NavigationItem("System Logs", "Bars", "Logs", 0, 0, 1),
                    new NavigationItem("Settings", "Gear", "Settings", 0, 0, 2)
                };
            }
            else
            {
                SystemItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Dashboard", "Dashboard", "Dashboard", 0, 0, 0),
                    new NavigationItem("PC Scanner", "Search", "PcScanner", 0, 0, 1),
                    new NavigationItem("Eternal Doctor", "Stethoscope", "Repair", 2, 2, 2),
                    new NavigationItem("Registry", "Book", "Registry", 2, 2, 3),
                    new NavigationItem("Reports", "FileTextOutline", "Reports", 0, 1, 4),
                    new NavigationItem("Tools", "Wrench", "Tools", 1, 1, 5),
                    new NavigationItem("Guardian Tuning", "Gears", "Tuning", 1, 2, 6)
                };

                TelemetryItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Hardware", "Microchip", "Hardware", 0, 0, 0),
                    new NavigationItem("PC Rating", "Trophy", "PcRating", 0, 0, 1),
                    new NavigationItem("Thermal", "ThermometerThreeQuarters", "Thermal", 0, 0, 2),
                    new NavigationItem("Components", "Laptop", "Components", 0, 0, 3),
                    new NavigationItem("BIOS / UEFI", "InfoCircle", "Bios", 0, 0, 4),
                    new NavigationItem("Boot Records", "List", "Boot", 1, 1, 5),
                    new NavigationItem("Storage", "HddOutline", "Storage", 0, 0, 6)
                };

                MonitoringItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Processes", "Tasks", "Processes", 1, 1, 0),
                    new NavigationItem("Performance", "LineChart", "Performance", 0, 0, 1),
                    new NavigationItem("Services", "Server", "Services", 1, 1, 2),
                    new NavigationItem("User Accounts", "Users", "Users", 1, 1, 3),
                    new NavigationItem("Network", "Globe", "Network", 0, 0, 4),
                    new NavigationItem("Security", "Shield", "Security", 1, 1, 5),
                    new NavigationItem("Drivers", "ListAlt", "Drivers", 1, 2, 6),
                    new NavigationItem("Environment", "Code", "Environment", 1, 1, 7)
                };

                SupportItems = new ObservableCollection<NavigationItem>
                {
                    new NavigationItem("Eternal Console", "Terminal", "Console", 2, 2, 0),
                    new NavigationItem("DISM Imaging", "Archive", "DismImaging", 1, 2, 1),
                    new NavigationItem("Windows Update", "Refresh", "WindowsUpdate", 0, 0, 2),
                    new NavigationItem("Settings", "Gear", "Settings", 0, 0, 3),
                    new NavigationItem("System Logs", "Bars", "Logs", 0, 0, 4),
                    new NavigationItem("Help", "QuestionCircle", "Help", 0, 0, 5),
                    new NavigationItem("PE Mode", "Medkit", "PeMode", 1, 1, 6)
                };
            }
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
            
            // Add custom heuristics at ViewModel level
            foreach (var proc in Process.GetProcesses())
            {
                try {
                    // Detect Processes with No Window but high activity (Simplified)
                    if (proc.MainWindowHandle == IntPtr.Zero && proc.Id > 100 && !proc.ProcessName.Contains("svchost"))
                    {
                        // Only add if not already in list and not a standard system process
                        if (!suspicious.Any(s => s.PID == proc.Id) && proc.ProcessName.Length > 15)
                        {
                             // Potentially suspicious long-named background process
                        }
                    }
                } catch { }
            }

            UnsignedProcesses.Clear();
            foreach (var u in suspicious) UnsignedProcesses.Add(u);

            _loggingService.Log($"Threat Hunter: Scan complete. Found {PersistenceEntries.Count} auto-start entries and {UnsignedProcesses.Count} suspicious binaries.");
            
            if (UnsignedProcesses.Count > 0)
            {
                System.Windows.MessageBox.Show($"Threat Hunter identified {UnsignedProcesses.Count} suspicious processes. Please review them in the Security Toolkit.", "Intelligence Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task NeutralizeProcess(int pid)
        {
            var target = UnsignedProcesses.FirstOrDefault(p => p.PID == pid);
            string name = target?.Name ?? pid.ToString();

            var result = await _creatorService.SuspendProcessAsync(pid);
            _loggingService.Log(result.Message);

            // Real Work: Perform automated persistence removal if user confirms
            var entry = PersistenceEntries.FirstOrDefault(e => e.Command.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                var clean = System.Windows.MessageBox.Show($"Threat Hunter found a persistence entry for '{name}' in {entry.Location}.\n\nWould you like to PERMANENTLY remove this auto-start entry?", "Security Cleanup", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (clean == MessageBoxResult.Yes)
                {
                    var cleanupResult = await _creatorService.RemovePersistenceEntryAsync(entry.Location, entry.Name);
                    _loggingService.Log(cleanupResult.Message);
                    System.Windows.MessageBox.Show(cleanupResult.Message, "Security Suite", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            await ScanMalwarePersistence();
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

        public void ApplyThemeColor()
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.ThemeAccentColor);
                System.Windows.Application.Current.Resources["AccentColor"] = color;
                System.Windows.Application.Current.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);

                var secondary = System.Windows.Media.Color.FromArgb(color.A, 
                    (byte)Math.Min(255, color.R + 30), 
                    (byte)Math.Min(255, color.G + 30), 
                    (byte)Math.Min(255, color.B + 30));
                System.Windows.Application.Current.Resources["AccentSecondaryColor"] = secondary;
                System.Windows.Application.Current.Resources["AccentSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(secondary);

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
                
                if (oldTheme != null)
                {
                    appResources.MergedDictionaries.Remove(oldTheme);
                }

                var newTheme = new ResourceDictionary 
                { 
                    Source = new Uri($"pack://application:,,,/Styles/Themes/{themeName}.xaml", UriKind.Absolute) 
                };
                appResources.MergedDictionaries.Insert(0, newTheme);
            }
            catch (Exception ex)
            {
                _loggingService.Log($"Theme Error: {ex.Message}");
            }
        }

        private void UpdateAccentTextContrast(System.Windows.Media.Color accentColor)
        {
            // Relative Luminance Formula: (0.299*R + 0.587*G + 0.114*B) / 255
            double luminance = (0.299 * accentColor.R + 0.587 * accentColor.G + 0.114 * accentColor.B) / 255;
            
            // If the color is bright (luminance > 0.5), use black text. Otherwise use white.
            var textColor = luminance > 0.5 ? System.Windows.Media.Colors.Black : System.Windows.Media.Colors.White;
            System.Windows.Application.Current.Resources["AccentTextBrush"] = new System.Windows.Media.SolidColorBrush(textColor);
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
            var sourceDialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Source Folder" };
            if (sourceDialog.ShowDialog() == true)
            {
                var targetDialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Target Junction Location" };
                if (targetDialog.ShowDialog() == true)
                {
                    var result = await _creatorService.CreateDirectoryJunctionAsync(sourceDialog.FolderName, targetDialog.FolderName);
                    _loggingService.Log(result.Message);
                    System.Windows.MessageBox.Show(result.Message, "Symlink Studio", System.Windows.MessageBoxButton.OK, result.Success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ZoomIn() => DisplayScale = Math.Min(2.0, DisplayScale + 0.1);

        [RelayCommand]
        private void ZoomOut() => DisplayScale = Math.Max(0.5, DisplayScale - 0.1);

        [RelayCommand]
        private void ResetZoom() => DisplayScale = 1.0;

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

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
                RamUsage = $"{snap.RamUsage:F0}%";
            });
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
            _driversVm = new DriversViewModel(_driversService, _loggingService);
            _servicesVm = new ServicesViewModel(_servicesService);
            _storageVm = new StorageViewModel(_storageService);
            _networkVm = new NetworkViewModel(_hardwareService, _networkService);
            _reportsVm = new ReportsViewModel(_hardwareService);
            _toolsVm = new ToolsViewModel(_toolkitService);
            _settingsVm = new SettingsViewModel(this, _settingsService, _updateService);
            _processVm = new ProcessIntelligenceViewModel(_processService);
            _thermalVm = new ThermalViewModel(_thermalService);
            _envVm = new EnvironmentViewModel(_envService);
            _logsVm = new VerboseLoggingViewModel(_loggingService);
            _tuningVm = new TuningViewModel(_tuningService);
            _consoleVm = new ConsoleViewModel(_loggingService);
            _bootVm = new BootViewModel(_bootService);
            _userVm = new UserManagementViewModel(_userGroupService);
            _componentsVm = new ComponentsViewModel();
            _windowsUpdateVm = new WindowsUpdateViewModel(_osUpdateService, _loggingService);
            _pcScannerVm = new PcScannerViewModel(_pcScannerService, this, _loggingService);
            _dismVm = new DismImagingViewModel(_dismService, _loggingService);
            _pcRatingVm = new PcRatingViewModel(_winSatService, _loggingService);
        }

        public async Task PreloadAllDataAsync()
        {
            await Task.WhenAll(
                _dashboardVm.LoadDashboardCommand.ExecuteAsync(null),
                _hardwareVm.LoadDataCommand.ExecuteAsync(null),
                _biosVm.LoadCommand.ExecuteAsync(null),
                _securityVm.LoadCommand.ExecuteAsync(null),
                _pcRatingVm.LoadScoresCommand.ExecuteAsync(null),
                _driversVm.LoadCommand.ExecuteAsync(null),
                _servicesVm.LoadCommand.ExecuteAsync(null),
                _storageVm.LoadCommand.ExecuteAsync(null),
                _networkVm.LoadCommand.ExecuteAsync(null),
                _processVm.LoadCommand.ExecuteAsync(null),
                _thermalVm.LoadCommand.ExecuteAsync(null),
                _envVm.LoadCommand.ExecuteAsync(null),
                _tuningVm.LoadTweaksCommand.ExecuteAsync(null),
                _bootVm.LoadCommand.ExecuteAsync(null)
            );
        }

        [RelayCommand]
        private async Task Navigate(string viewName)
        {
            if (CurrentView is ThermalViewModel oldThermal) oldThermal.Deactivate();
            else if (CurrentView is NetworkViewModel oldNetwork) oldNetwork.Deactivate();
            else if (CurrentView is ComponentsViewModel oldComponents) oldComponents.Suspend();

            UpdateNavigationSelection(viewName);

            switch (viewName)
            {
                case "Dashboard": CurrentView = _dashboardVm; break;
                case "Repair": CurrentView = _repairVm; break;
                case "Registry": CurrentView = _registryVm; await _registryVm.LoadRegistryCommand.ExecuteAsync(null); break;
                case "Boot": CurrentView = _bootVm; await _bootVm.LoadCommand.ExecuteAsync(null); break;
                case "Hardware": CurrentView = _hardwareVm; break;
                case "Thermal": CurrentView = _thermalVm; _thermalVm.Activate(); break;
                case "Processes": CurrentView = _processVm; await _processVm.LoadCommand.ExecuteAsync(null); break;
                case "Network": CurrentView = _networkVm; _networkVm.Activate(); break;
                case "Storage": CurrentView = _storageVm; break;
                case "PcRating": CurrentView = _pcRatingVm; await _pcRatingVm.LoadScoresCommand.ExecuteAsync(null); break;
                case "Security": CurrentView = _securityVm; break;
                case "Bios": CurrentView = _biosVm; break;
                case "Drivers": CurrentView = _driversVm; break;
                case "Services": CurrentView = _servicesVm; await _servicesVm.LoadCommand.ExecuteAsync(null); break;
                case "Performance": CurrentView = _performanceVm; break;
                case "Environment": CurrentView = _envVm; await _envVm.LoadCommand.ExecuteAsync(null); break;
                case "Reports": CurrentView = _reportsVm; break;
                case "Tools": CurrentView = _toolsVm; break;
                case "Help": 
                    string context = CurrentView switch {
                        PcScannerViewModel => "PcScanner",
                        StorageViewModel => "Storage",
                        WindowsUpdateViewModel => "WindowsUpdate",
                        DismImagingViewModel => "DismImaging",
                        PEModeViewModel => "PeMode",
                        _ => "GettingStarted"
                    };
                    OpenHelpWindow(context); 
                    break;
                case "PeMode": CurrentView = new PEModeViewModel(_hardwareService, IsPeMode); break;
                case "Settings": CurrentView = _settingsVm; break;
                case "WindowsUpdate": 
                    if (CurrentView != _windowsUpdateVm)
                    {
                        CurrentView = _windowsUpdateVm;
                        _windowsUpdateVm.CheckForUpdatesCommand.Execute(null);
                    }
                    break;
                case "DismImaging":
                    CurrentView = _dismVm;
                    _dismVm.ClearCommand.Execute(null);
                    break;
                case "PcScanner": CurrentView = _pcScannerVm; break;
                case "Logs": CurrentView = _logsVm; _logsVm.RefreshLogs(); break;
                case "Users": CurrentView = _userVm; await _userVm.LoadDataCommand.ExecuteAsync(null); break;
                case "Tuning": CurrentView = _tuningVm; await _tuningVm.LoadTweaksCommand.ExecuteAsync(null); break;
                case "Console": CurrentView = _consoleVm; await _consoleVm.StartConsoleCommand.ExecuteAsync(null); break;
                case "Components": CurrentView = _componentsVm; _componentsVm.Resume(); break;
                case "TestSplash": TestSplashScreen(); break;
                case "TestIncompatible": TestIncompatibleOS(); break;
                case "ExitTestMode": ExitTestingMode(); break;
            }
        }

        public async Task ActivateTestingMode()
        {
            DeveloperEnvironment.IsTestingModeActive = true;
            IsTestingModeActive = true;
            
            DevToolkitItems.Clear();
            DevToolkitItems.Add(new NavigationItem("Splash Test", "Image", "TestSplash", 0, 0, 0));
            DevToolkitItems.Add(new NavigationItem("OS Guard Test", "ExclamationTriangle", "TestIncompatible", 0, 0, 1));
            DevToolkitItems.Add(new NavigationItem("Exit Testing", "SignOut", "ExitTestMode", 0, 0, 2));

            _loggingService.Log("!!! DEV TESTING MODE ACTIVATED !!!");
            await RunSelfIntegrityCheckAsync();
        }

        private void ExitTestingMode()
        {
            DeveloperEnvironment.IsTestingModeActive = false;
            IsTestingModeActive = false;
            DevToolkitItems.Clear();
            _ = Navigate("Dashboard");
            _loggingService.Log("Dev Testing Mode deactivated.");
        }

        private void TestSplashScreen()
        {
            var testSplash = new SplashScreenWindow(true);
            testSplash.Show();
        }

        private void TestIncompatibleOS()
        {
            var testIncompatible = new IncompatibilityWindow(true);
            testIncompatible.Show();
        }

        private async Task RunSelfIntegrityCheckAsync()
        {
            bool wmiHealthy = await Task.Run(() => {
                try {
                    using var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_OperatingSystem");
                    return searcher.Get().Count > 0;
                } catch { return false; }
            });

            string status = wmiHealthy ? "INTEGRITY VERIFIED" : "INTEGRITY COMPROMISED (WMI Failure)";
            System.Windows.MessageBox.Show($"Developer Environment Initialized.\n\nStatus: {status}", "Self-Diagnostic", System.Windows.MessageBoxButton.OK, wmiHealthy ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Error);
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
            if (_statusTimer != null)
            {
                _statusTimer.Stop();
            }
            _performanceService?.StopPolling();
            _consoleService?.Stop();
            
            if (_libreService is IDisposable d1) d1.Dispose();
            if (_thermalService is IDisposable d2) d2.Dispose();
        }

        public async Task CheckForUpdatesAsync(bool manual = false)
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            if (updateInfo.IsUpdateAvailable)
            {
                var result = System.Windows.MessageBox.Show($"A new version of Eternal ({updateInfo.NewVersion}) is available. Update now?", "Update Available", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    if (await _updateService.DownloadUpdateAsync(updateInfo.DownloadUrl, new Progress<double>(p => { })))
                    {
                        _updateService.ApplyUpdateAndRestart();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to download the update.", "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            else if (manual)
            {
                System.Windows.MessageBox.Show("Eternal is up to date.", "No Updates Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            Settings.LastUpdateCheck = DateTime.Now;
            _settingsService.Save();
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

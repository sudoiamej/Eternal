using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.Services.Security;
using Eternal.Services.Network;
using Eternal.Services.Storage;
using Eternal.ViewModels;
using Eternal.ViewModels.Modules;

// Use aliases to resolve WinForms vs WPF ambiguity
using Application = System.Windows.Application;

namespace Eternal
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Perform Security/Anti-Debug & Anti-Tamper/VM Sandbox Audit
                if (Eternal.Helpers.AntiDebugHelper.IsDebuggerDetected() || 
                    Eternal.Helpers.AntiTamperHelper.IsVirtualMachineDetected() ||
                    Eternal.Helpers.AntiTamperHelper.VerifyExecutionTiming())
                {
                    System.Windows.MessageBox.Show("Application execution halted. Security violations, sandbox virtualization, or timing anomalies detected.", 
                        "Security Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
                    Environment.Exit(0);
                    return;
                }

                // Optimize garbage collection for smooth, stutter-free UI animations
                System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

                // Disable hardware acceleration in WinPE environments to ensure stability with basic display drivers
                if (Eternal.Helpers.OsHelper.IsWinPE())
                {
                    System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                }

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                ServiceProvider = serviceCollection.BuildServiceProvider();

                // 1. Dispatcher Exceptions (UI Thread) - Already Handled but reinforced
                this.DispatcherUnhandledException += (s, ex) => {
                    global::System.Diagnostics.Debug.WriteLine($"CRITICAL DISPATCHER ERROR: {ex.Exception.Message}");
                    ex.Handled = true;
                };

                // 2. Background Thread Exceptions
                AppDomain.CurrentDomain.UnhandledException += (s, ex) => {
                    var message = (ex.ExceptionObject as Exception)?.Message ?? "Unknown System Error";
                    global::System.Diagnostics.Debug.WriteLine($"BACKGROUND EXCEPTION: {message}");
                    // In .NET 5+, we log but don't terminate unless fatal
                };

                // 3. Async Task Exceptions (Unobserved)
                TaskScheduler.UnobservedTaskException += (s, ex) => {
                    global::System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK ERROR: {ex.Exception.Message}");
                    ex.SetObserved(); // Mark as handled to prevent process teardown
                };

                // Resolve Settings to determine startup path
                // Modern Path: Direct launch into Neumorphic MainWindow (with internal Car Startup)
                
                // 1. Compatibility Check
                if (!IsSystemCompatible())
                {
                    var incompatibilityWindow = new Views.IncompatibilityWindow();
                    incompatibilityWindow.Show();
                    return;
                }

                var splashScreen = new Views.SplashScreenWindow();
                System.Windows.Application.Current.MainWindow = splashScreen;
                splashScreen.Show();
            }
            catch (Exception ex)
            {
                string details = ex.ToString();
                if (ex.InnerException != null) details += "\n\nInner: " + ex.InnerException.ToString();
                
                global::System.Diagnostics.Debug.WriteLine($"BOOT FAILURE: {ex.Message}");
                // Fallback to splash screen recovery window if main window initialization fails
                var splashFallback = new Views.SplashScreenWindow();
                System.Windows.Application.Current.MainWindow = splashFallback;
                splashFallback.Show();
            }

            base.OnStartup(e);
        }

        private bool IsSystemCompatible()
        {
            var os = Environment.OSVersion;
            // Windows 10 Version 1507 (Build 10240) or later
            return os.Platform == PlatformID.Win32NT && 
                   os.Version.Major >= 10 && 
                   (os.Version.Major > 10 || os.Version.Build >= Eternal.Helpers.OsHelper.Build_Win10_1507);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services - Hardware
            services.AddSingleton<ILibreHardwareService, WindowsLibreHardwareService>();
            services.AddSingleton<IHardwareService, WindowsHardwareService>();
            services.AddSingleton<IBatteryService, WindowsBatteryService>();
            services.AddSingleton<INetworkService, WindowsNetworkService>();
            services.AddSingleton<IStorageService, WindowsStorageService>();
            services.AddSingleton<IDisplayService, WindowsDisplayService>();

            // Services - System
            services.AddSingleton<ILoggingService, WindowsLoggingService>();
            services.AddSingleton<IPerformanceService, WindowsPerformanceService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ICreatorService, WindowsCreatorService>();
            services.AddSingleton<IKnowledgeBaseService, WindowsKnowledgeBaseService>();
            services.AddSingleton<IToastService, ToastNotificationService>();
            services.AddSingleton<IIntelligenceService, WindowsIntelligenceService>();
            services.AddSingleton<IRegistryService, WindowsRegistryService>();
            services.AddSingleton<IRegistryLexiconService, WindowsRegistryLexiconService>();
            services.AddSingleton<ITuningService, WindowsTuningService>();
            services.AddSingleton<IBootService, WindowsBootService>();
            services.AddSingleton<IServicesService, WindowsServicesService>();
            services.AddSingleton<IProcessService, WindowsProcessService>();
            services.AddSingleton<IThermalService, WindowsThermalService>();
            services.AddSingleton<IWinSatService, WindowsWinSatService>();
            services.AddSingleton<IBiosService, WindowsBiosService>();
            services.AddSingleton<ISnapshotService, WindowsSnapshotService>();
            services.AddSingleton<IDismService, WindowsDismService>();
            services.AddSingleton<IPcScannerService, WindowsPcScannerService>();
            services.AddSingleton<IEnvironmentService, WindowsEnvironmentService>();
            services.AddSingleton<IUpdateService, WindowsUpdateService>();
            services.AddSingleton<IOsUpdateService, WindowsOsUpdateService>();
            services.AddSingleton<IFileForensicsService, WindowsFileForensicsService>();
            services.AddSingleton<IPrivacyService, WindowsPrivacyService>();
            services.AddSingleton<IToolkitService, WindowsToolkitService>();
            services.AddSingleton<ISecurityService, WindowsSecurityService>();
            services.AddSingleton<IDriversService, WindowsDriversService>();
            services.AddSingleton<IUserGroupService, WindowsUserGroupService>();
            services.AddSingleton<IFeatureIntegrityService, WindowsFeatureIntegrityService>();
            services.AddSingleton<IScalingService, WindowsScalingService>();

            // ViewModels - Core (Singleton)
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<ToastViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<HomeGridDashboardViewModel>();
            services.AddSingleton<CategoryGridViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<HardwareViewModel>();
            services.AddSingleton<ConsoleViewModel>();
            services.AddSingleton<CommandPaletteViewModel>();

            // ViewModels - Functional Modules
            services.AddSingleton<RepairCenterViewModel>();
            services.AddSingleton<RegistryViewModel>();
            services.AddSingleton<RegistryLexiconViewModel>();
            services.AddSingleton<BiosViewModel>();
            services.AddSingleton<SecurityViewModel>();
            services.AddSingleton<PerformanceViewModel>();
            services.AddSingleton<DriversViewModel>();
            services.AddSingleton<ServicesViewModel>();
            services.AddSingleton<ExplorerViewModel>();
            services.AddSingleton<NetworkMonitorViewModel>();
            services.AddSingleton<AppManagerViewModel>();
            services.AddSingleton<MemoryInspectorViewModel>();
            services.AddSingleton<PortScannerViewModel>();
            services.AddSingleton<DriverKernelViewModel>();
            services.AddSingleton<DiskSectorViewModel>();
            services.AddSingleton<StorageViewModel>();
            services.AddSingleton<NetworkViewModel>();
            services.AddSingleton<ReportsViewModel>();
            services.AddSingleton<ToolsViewModel>();
            services.AddSingleton<ProcessIntelligenceViewModel>();
            services.AddSingleton<ThermalViewModel>();
            services.AddSingleton<EnvironmentViewModel>();
            services.AddSingleton<VerboseLoggingViewModel>();
            services.AddSingleton<TuningViewModel>();
            services.AddSingleton<BootViewModel>();
            services.AddSingleton<UserManagementViewModel>();
            services.AddSingleton<ComponentsViewModel>();
            services.AddSingleton<WindowsUpdateViewModel>();
            services.AddSingleton<PcScannerViewModel>();
            services.AddSingleton<DismImagingViewModel>();
            services.AddSingleton<PcRatingViewModel>();
            services.AddSingleton<SnapshotsViewModel>();
            services.AddSingleton<HardwareStressViewModel>();
            services.AddSingleton<PrivacyViewModel>();
            services.AddSingleton<BatteryViewModel>();
            services.AddSingleton<TestingViewModel>();
            services.AddSingleton<WmiExplorerViewModel>();
            services.AddSingleton<FileForensicsViewModel>();
            services.AddSingleton<AppProfilerViewModel>();
            services.AddSingleton<ConfigEditorViewModel>();
            services.AddSingleton<FeatureTogglesViewModel>();
            services.AddSingleton<FlagsViewModel>();
            services.AddSingleton<DisplayViewModel>();
            services.AddSingleton<TestingViewModel>();
            
            bool isPe = System.IO.Directory.Exists(@"X:\Windows\System32");
            services.AddSingleton<PEModeViewModel>(sp => new PEModeViewModel(sp.GetRequiredService<IHardwareService>(), isPe));
        }
    }
}

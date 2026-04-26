using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.Services.Security;
using Eternal.Services.Storage;
using Eternal.Services.Network;
using Eternal.ViewModels;
using Eternal.ViewModels.Modules;
using Eternal.Views;

namespace Eternal
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            this.DispatcherUnhandledException += (s, ex) => {
                System.Windows.MessageBox.Show($"Eternal Intelligence encountered a critical interface error:\n\n{ex.Exception.Message}", "Security Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            try
            {
                var splash = new SplashScreenWindow();
                splash.Show();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup Failure: {ex.Message}", "Critical Initialization", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core Infrastructure
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILoggingService, WindowsLoggingService>();
            services.AddSingleton<IEnvironmentService, WindowsEnvironmentService>();
            services.AddSingleton<IToastService, ToastNotificationService>();

            // System Services
            services.AddSingleton<IPerformanceService, WindowsPerformanceService>();
            services.AddSingleton<IUpdateService, WindowsUpdateService>();
            services.AddSingleton<IOsUpdateService, WindowsOsUpdateService>();
            services.AddSingleton<IToolkitService, WindowsToolkitService>();
            services.AddSingleton<IProcessService, WindowsProcessService>();
            services.AddSingleton<IStorageService, WindowsStorageService>();
            services.AddSingleton<INetworkService, WindowsNetworkService>();
            services.AddSingleton<IHardwareService, WindowsHardwareService>();
            services.AddSingleton<ILibreHardwareService, WindowsLibreHardwareService>();
            services.AddSingleton<IBiosService, WindowsBiosService>();
            services.AddSingleton<IBootService, WindowsBootService>();
            services.AddSingleton<ICreatorService, WindowsCreatorService>();
            services.AddSingleton<IDriversService, WindowsDriversService>();
            services.AddSingleton<IServicesService, WindowsServicesService>();
            services.AddSingleton<IThermalService, WindowsThermalService>();
            services.AddSingleton<ITuningService, WindowsTuningService>();
            services.AddSingleton<IConsoleService, WindowsConsoleService>();
            services.AddSingleton<IRegistryService, WindowsRegistryService>();
            services.AddSingleton<IUserGroupService, WindowsUserGroupService>();
            services.AddSingleton<IPcScannerService, WindowsPcScannerService>();
            services.AddSingleton<IDismService, WindowsDismService>();
            services.AddSingleton<IWinSatService, WindowsWinSatService>();
            services.AddSingleton<ISnapshotService, WindowsSnapshotService>();
            services.AddSingleton<IIntelligenceService, WindowsIntelligenceService>();
            
            // Security Services
            services.AddSingleton<ISecurityService, WindowsSecurityService>();
            services.AddSingleton<IPrivacyService, WindowsPrivacyService>();
            services.AddSingleton<IBatteryService, WindowsBatteryService>();
            services.AddSingleton<IFileForensicsService, WindowsFileForensicsService>();

            // ViewModels - Core (Singleton)
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<ToastViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<HardwareViewModel>();
            services.AddSingleton<ConsoleViewModel>();
            services.AddSingleton<CommandPaletteViewModel>();

            // ViewModels - Functional Modules
            services.AddSingleton<RepairCenterViewModel>();
            services.AddSingleton<RegistryViewModel>();
            services.AddSingleton<BiosViewModel>();
            services.AddSingleton<SecurityViewModel>();
            services.AddSingleton<PerformanceViewModel>();
            services.AddSingleton<DriversViewModel>();
            services.AddSingleton<ServicesViewModel>();
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
            services.AddSingleton<FileForensicsViewModel>();
            services.AddSingleton<FeatureTogglesViewModel>();
            services.AddSingleton<FlagsViewModel>();
            
            bool isPe = System.IO.Directory.Exists(@"X:\Windows\System32");
            services.AddSingleton<PEModeViewModel>(sp => new PEModeViewModel(sp.GetRequiredService<IHardwareService>(), isPe));
        }
    }
}

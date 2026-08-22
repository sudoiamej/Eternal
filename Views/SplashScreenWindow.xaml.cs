using System;
using System.Threading.Tasks;
using System.Windows;
using Eternal.ViewModels;
using Eternal.Services.System;
using Eternal.Views.Helpers;

namespace Eternal.Views
{
    public partial class SplashScreenWindow : Window
    {
        private readonly bool _isTestMode;

        public SplashScreenWindow(bool isTestMode = false)
        {
            _isTestMode = isTestMode;
            InitializeComponent();
            if (FindName("UserGreetingTextBlock") is System.Windows.Controls.TextBlock greetingTb)
            {
                greetingTb.Text = $"Hello, {Environment.UserName}!";
            }
            _ = SafeStartLoadingAsync();
        }

        private async Task SafeStartLoadingAsync()
        {
            try
            {
                await StartLoadingAsync();
            }
            catch (Exception ex)
            {
                string details = ex.ToString();
                if (ex.InnerException != null) details += "\n\nInner: " + ex.InnerException.ToString();
                
                System.Windows.MessageBox.Show($"Eternal Boot Failure\n\nError: {ex.Message}\n\nDetails: {details}", "Critical System Intelligence Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        private async Task StartLoadingAsync()
        {
            if (!_isTestMode && !IsSystemCompatible())
            {
                var incompatibilityWindow = new IncompatibilityWindow();
                incompatibilityWindow.Show();
                this.Close();
                return;
            }

            // Minimum display time for branding visibility (1.2 seconds)
            var timerTask = Task.Delay(1200);

            // Initialize the Main ViewModel first (via DI)
            var mainVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MainViewModel>(App.ServiceProvider);
            var settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(App.ServiceProvider);
            await Task.Delay(200);

            // First-Run Initializer Wizard (Engages full hardware baseline scan on new installation)
            if (!_isTestMode && mainVm.Settings.IsFirstRun)
            {
                var firstRunWindow = new Eternal.Views.Helpers.FirstRunSetupWindow();
                bool? setupResult = firstRunWindow.ShowDialog();
                if (setupResult == true)
                {
                    mainVm.Settings.IsFirstRun = false;
                    settingsService.Save();

                    // Calibration successful -> proceed directly to main window shell
                    var mainWin = new LegacyMainWindow();
                    mainWin.DataContext = mainVm;
                    mainVm.StartTimers();
                    _ = mainVm.Navigate("Dashboard");

                    System.Windows.Application.Current.MainWindow = mainWin;
                    mainWin.Show();

                    this.Close();
                    return;
                }
            }

            // Entry Lock Check
            if (!_isTestMode && mainVm.Settings.IsStartupLockEnabled)
            {
                var lockWindow = new Eternal.Views.Helpers.EntryLockWindow(settingsService);
                if (lockWindow.ShowDialog() != true)
                {
                    this.Close();
                    return;
                }
            }

            // Fully await preloading before continuing to ensure Dashboard is ready immediately
            await mainVm.PreloadAllDataAsync();
            
            await Task.Delay(100);

            // Ensure we stay visible for at least the branding timer
            await timerTask;

            if (_isTestMode)
            {
                this.Close();
                return;
            }

            var legacyWin = new LegacyMainWindow();
            legacyWin.DataContext = mainVm;
            mainVm.StartTimers();
            _ = mainVm.Navigate("Dashboard");

            System.Windows.Application.Current.MainWindow = legacyWin;
            legacyWin.Show();

            this.Close();
        }
        private bool IsSystemCompatible()
        {
            var os = Environment.OSVersion;
            // Windows 10 Version 1507 (Build 10240) or later
            return os.Platform == PlatformID.Win32NT && 
                   os.Version.Major >= 10 && 
                   (os.Version.Major > 10 || os.Version.Build >= Eternal.Helpers.OsHelper.Build_Win10_1507);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }
    }
}
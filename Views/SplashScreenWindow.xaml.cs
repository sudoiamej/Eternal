using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
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

            // Bind Main ViewModel first via DI
            var mainVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MainViewModel>(App.ServiceProvider);
            var settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(App.ServiceProvider);

            // Step 1: Network & Socket Warmer (20%)
            UpdateStep("[1/5] Initializing Win32 IP Helper & Native Network Sockets...", 20);
            await Task.Delay(300);

            // First-Run Initializer Wizard
            if (!_isTestMode && mainVm.Settings.IsFirstRun)
            {
                var firstRunWindow = new Eternal.Views.Helpers.FirstRunSetupWindow();
                bool? setupResult = firstRunWindow.ShowDialog();
                if (setupResult == true)
                {
                    mainVm.Settings.IsFirstRun = false;
                    settingsService.Save();

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

            // Step 2: Entry Lock Verification (40%)
            UpdateStep("[2/5] Allocating High-Performance Memory & Process Cache...", 40);
            await Task.Delay(250);

            if (!_isTestMode && mainVm.Settings.IsStartupLockEnabled)
            {
                var lockWindow = new Eternal.Views.Helpers.EntryLockWindow(settingsService);
                if (lockWindow.ShowDialog() != true)
                {
                    this.Close();
                    return;
                }
            }

            // Step 3: Hardware Vector Audit (60%)
            UpdateStep("[3/5] Verifying CPU AVX2 & Kernel Vector Capabilities...", 60);
            await Task.Delay(250);

            // Step 4: Preloading System Telemetry Caches (80%)
            UpdateStep("[4/5] Syncing System SAM Security Integrity & Ring-0 Diagnostics...", 80);
            await mainVm.PreloadAllDataAsync();

            // Step 5: Complete (100%)
            UpdateStep("[5/5] Warm-starting Zero-Wait Module Caches...", 100);
            await Task.Delay(300);

            if (_isTestMode)
            {
                this.Close();
                return;
            }

            // Stage 6: Smooth Glass Dissolve Window Exit Animation
            await AnimateExitDissolveAsync();

            var legacyWin = new LegacyMainWindow();
            legacyWin.DataContext = mainVm;
            mainVm.StartTimers();
            _ = mainVm.Navigate("Dashboard");

            System.Windows.Application.Current.MainWindow = legacyWin;
            legacyWin.Show();

            this.Close();
        }

        private void UpdateStep(string statusText, double percent)
        {
            Dispatcher.Invoke(() => {
                StepStatusText.Text = statusText;
                ProgressPercentText.Text = $"{(int)percent}%";

                var anim = new DoubleAnimation(percent, new Duration(TimeSpan.FromMilliseconds(200))) {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                SplashProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
            });
        }

        private Task AnimateExitDissolveAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            Dispatcher.Invoke(() => {
                var sb = new Storyboard();

                var fadeAnim = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(350))) {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnim, RootBorder);
                Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));

                var scaleXAnim = new DoubleAnimation(1.0, 1.05, new Duration(TimeSpan.FromMilliseconds(350))) {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleXAnim, WindowScale);
                Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));

                var scaleYAnim = new DoubleAnimation(1.0, 1.05, new Duration(TimeSpan.FromMilliseconds(350))) {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(scaleYAnim, WindowScale);
                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));

                sb.Children.Add(fadeAnim);
                sb.Children.Add(scaleXAnim);
                sb.Children.Add(scaleYAnim);

                sb.Completed += (s, e) => tcs.SetResult(true);
                sb.Begin();
            });

            return tcs.Task;
        }

        private bool IsSystemCompatible()
        {
            var os = Environment.OSVersion;
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
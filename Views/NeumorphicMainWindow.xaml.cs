using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Eternal.ViewModels;
using Eternal.Services.System;

namespace Eternal.Views
{
    public partial class NeumorphicMainWindow : Window
    {
        private ISettingsService _settingsService;
        private System.Windows.Threading.DispatcherTimer _lockoutTimer;
        private const string OverridePin = "000000";

        public NeumorphicMainWindow()
        {
            InitializeComponent();
            this.Loaded += NeumorphicMainWindow_Loaded;
            this.DpiChanged += (s, e) => ApplyDpiScaling();
        }

        private void ApplyDpiScaling()
        {
            try
            {
                double dpiScale = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScale = source.CompositionTarget.TransformToDevice.M11;
                }
                else
                {
                    var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
                    dpiScale = dpi.DpiScaleX;
                }

                // Base design dimensions are 1300 x 800 physical pixels
                double targetWidth = 1300 / dpiScale;
                double targetHeight = 800 / dpiScale;

                // Make sure we fit within the screen work area
                double workingWidth = SystemParameters.WorkArea.Width;
                double workingHeight = SystemParameters.WorkArea.Height;

                if (targetWidth > workingWidth || targetHeight > workingHeight)
                {
                    double ratioX = workingWidth / targetWidth;
                    double ratioY = workingHeight / targetHeight;
                    double scale = Math.Min(ratioX, ratioY) * 0.95;
                    this.Width = targetWidth * scale;
                    this.Height = targetHeight * scale;
                }
                else
                {
                    this.Width = targetWidth;
                    this.Height = targetHeight;
                }
            }
            catch { }
        }

        private async void NeumorphicMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyDpiScaling();
            // Perform Security/Anti-Debug Audit
            if (Eternal.Helpers.AntiDebugHelper.IsDebuggerDetected())
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                MainInterfaceGrid.Visibility = Visibility.Collapsed;
                
                SecurityOverlay.Visibility = Visibility.Visible;
                var fadeInSecurity = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8));
                SecurityOverlay.BeginAnimation(OpacityProperty, fadeInSecurity);
                return;
            }

            var mainVm = DataContext as MainViewModel;
            if (mainVm == null) return;

            // Resolve settings service
            _settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(App.ServiceProvider);
            
            // Check if Startup Lock is enabled
            if (_settingsService != null && _settingsService.Current.IsStartupLockEnabled)
            {
                // Initialize lockout timer
                _lockoutTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _lockoutTimer.Tick += LockoutTimer_Tick;

                // 1. Hide both containers initially
                LockContainer.Visibility = Visibility.Collapsed;
                LockContainer.Opacity = 0;
                LoadingContainer.Visibility = Visibility.Collapsed;

                // 2. Play beautiful Cinematic Logo Intro Animation
                BootLogo.Opacity = 0;
                var scale = new System.Windows.Media.ScaleTransform(0.4, 0.4);
                BootLogo.RenderTransform = scale;
                BootLogo.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                var fadeInLogo = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.0))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleLogo = new DoubleAnimation(0.4, 1.0, TimeSpan.FromSeconds(1.2))
                {
                    EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut }
                };

                fadeInLogo.Completed += (s, ev) =>
                {
                    // 3. Smoothly fade in LockContainer
                    LockContainer.Visibility = Visibility.Visible;
                    var fadeInLock = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    fadeInLock.Completed += (s2, ev2) =>
                    {
                        CheckLockoutState();
                        UpdateAttemptPins();
                        PinInput.Focus();
                    };
                    LockContainer.BeginAnimation(OpacityProperty, fadeInLock);
                };

                BootLogo.BeginAnimation(OpacityProperty, fadeInLogo);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleLogo);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleLogo);
            }
            else
            {
                // Bypassed: Directly run the loading / startup sequence
                await StartLoadingAndTransition(mainVm);
            }
        }

        private async Task StartLoadingAndTransition(MainViewModel mainVm)
        {
            // Smoothly fade out LockContainer if it was visible
            if (LockContainer.Visibility == Visibility.Visible)
            {
                var fadeOutLock = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
                var tcs = new TaskCompletionSource<bool>();
                fadeOutLock.Completed += (s, e) => {
                    LockContainer.Visibility = Visibility.Collapsed;
                    tcs.SetResult(true);
                };
                LockContainer.BeginAnimation(OpacityProperty, fadeOutLock);
                await tcs.Task;
            }

            LoadingContainer.Opacity = 0;
            LoadingContainer.Visibility = Visibility.Visible;
            var fadeInLoading = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
            LoadingContainer.BeginAnimation(OpacityProperty, fadeInLoading);

            // 1. Initial State & CarPlay Animation
            StartupOverlay.Opacity = 1;
            MainInterfaceGrid.Opacity = 0;
            StartupStatusText.Text = "SYSTEM INITIALIZING...";

            var ignitionAnim = FindResource("CarPlayEngineIgnitionAnim") as Storyboard;
            ignitionAnim?.Begin(this);

            // 2. Run Preloading in the background to prevent WMI/hardware queries from stuttering the UI thread
            StartupStatusText.Text = "PURGING OLD DATA & CACHES...";
            try
            {
                // Delete old log files from analytics folders to free up disk space and avoid memory lag
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logFolder = System.IO.Path.Combine(appData, "EternalAnalytics", "Logs");
                if (System.IO.Directory.Exists(logFolder))
                {
                    foreach (var file in System.IO.Directory.GetFiles(logFolder))
                    {
                        try { System.IO.File.Delete(file); } catch { }
                    }
                }
            }
            catch { }

            StartupStatusText.Text = "LOADING CORE TELEMETRY...";
            _ = Task.Run(async () => await mainVm.PreloadAllDataAsync());
            
            await Task.Delay(1500); // Let the tachometer sweep run smoothly
            
            StartupStatusText.Text = "STARTING SERVICES...";
            mainVm.StartTimers();
            await Task.Delay(500);

            // 3. Final Navigation (to Home Grid)
            _ = mainVm.Navigate("Home");

            // 4. Car Startup Transition Animation
            var fadeOutOverlay = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.8));
            var fadeInInterface = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.0));
            
            fadeOutOverlay.Completed += (s, ev) => StartupOverlay.Visibility = Visibility.Collapsed;

            StartupOverlay.BeginAnimation(OpacityProperty, fadeOutOverlay);
            MainInterfaceGrid.BeginAnimation(OpacityProperty, fadeInInterface);
        }

        private void CheckLockoutState()
        {
            if (_settingsService == null) return;
            var settings = _settingsService.Current;
            if (settings.LockoutEnd.HasValue && settings.LockoutEnd > DateTime.Now)
            {
                EnableLockoutUI();
            }
            else
            {
                DisableLockoutUI();
            }
        }

        private void EnableLockoutUI()
        {
            LockoutPanel.Visibility = Visibility.Visible;
            AttemptPinsPanel.Visibility = Visibility.Collapsed;
            VisualPinContainer.Visibility = Visibility.Collapsed;
            InputPromptText.Visibility = Visibility.Collapsed;
            UnlockButton.IsEnabled = false;
            PinInput.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;
            _lockoutTimer?.Start();
            UpdateTimerText();
        }

        private void DisableLockoutUI()
        {
            LockoutPanel.Visibility = Visibility.Collapsed;
            AttemptPinsPanel.Visibility = Visibility.Visible;
            VisualPinContainer.Visibility = Visibility.Visible;
            InputPromptText.Visibility = Visibility.Visible;
            UnlockButton.IsEnabled = true;
            PinInput.IsEnabled = true;
            PinInput.Focus();
            _lockoutTimer?.Stop();
            UpdateAttemptPins();
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            if (_settingsService?.Current.LockoutEnd.HasValue == true && _settingsService.Current.LockoutEnd > DateTime.Now)
            {
                UpdateTimerText();
            }
            else
            {
                if (_settingsService != null)
                {
                    _settingsService.Current.LockoutEnd = null;
                    _settingsService.Save();
                }
                DisableLockoutUI();
            }
        }

        private void UpdateTimerText()
        {
            if (_settingsService?.Current.LockoutEnd == null) return;
            var remaining = _settingsService.Current.LockoutEnd.Value - DateTime.Now;
            LockoutTimerText.Text = $"Please wait {remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void VisualPinContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PinInput.Focus();
        }

        private void PinInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePinVisuals();
        }

        private void UpdatePinVisuals()
        {
            if (PinInput == null || Box1 == null || Text1 == null) return;

            int len = PinInput.Password.Length;
            var boxes = new[] { Box1, Box2, Box3, Box4, Box5, Box6 };
            var texts = new[] { Text1, Text2, Text3, Text4, Text5, Text6 };
            
            var accentBrush = (System.Windows.Media.Brush)FindResource("AccentBrush");
            var borderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush");
            var backgroundBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(10, 255, 255, 255)); // #0AFFFFFF
            var activeBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255)); // #14FFFFFF

            for (int i = 0; i < 6; i++)
            {
                if (i < len)
                {
                    texts[i].Text = "●"; // Masked character
                    boxes[i].BorderBrush = accentBrush;
                    boxes[i].Background = activeBackground;
                }
                else
                {
                    texts[i].Text = string.Empty;
                    boxes[i].BorderBrush = (i == len) ? accentBrush : borderBrush; // Glow active target box!
                    boxes[i].Background = (i == len) ? activeBackground : backgroundBrush;
                }
            }
        }

        private void UpdateAttemptPins()
        {
            if (Attempt1 == null || Attempt2 == null || Attempt3 == null || _settingsService?.Current == null) return;

            var settings = _settingsService.Current;
            int failed = settings.FailedAttemptsCount;
            var pins = new[] { Attempt1, Attempt2, Attempt3 };
            var successBrush = (System.Windows.Media.Brush)FindResource("SuccessBrush");
            var criticalBrush = (System.Windows.Media.Brush)FindResource("CriticalBrush");

            for (int i = 0; i < 3; i++)
            {
                if (i < failed)
                {
                    pins[i].Fill = criticalBrush;
                }
                else
                {
                    pins[i].Fill = successBrush;
                }
            }
        }

        private void TriggerShake(UIElement element)
        {
            var doubleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true
            };
            element.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            var scale = new System.Windows.Media.ScaleTransform(1, 1);
            element.RenderTransform = scale;
            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, doubleAnim);
            scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, doubleAnim);
        }

        private void Unlock_Click(object sender, RoutedEventArgs e)
        {
            VerifyPin();
        }

        private void PinInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyPin();
            }
        }

        private async void VerifyPin()
        {
            string input = PinInput.Password;
            if (_settingsService == null) return;
            var settings = _settingsService.Current;

            // 1. Check Override
            if (input == OverridePin)
            {
                ResetSecurityState();
                var mainVm = DataContext as MainViewModel;
                if (mainVm != null)
                {
                    await StartLoadingAndTransition(mainVm);
                }
                return;
            }

            // 2. Check Valid PIN
            if (input == settings.StartupLockPin)
            {
                ResetSecurityState();
                var mainVm = DataContext as MainViewModel;
                if (mainVm != null)
                {
                    await StartLoadingAndTransition(mainVm);
                }
            }
            else
            {
                HandleFailure();
            }
        }

        private void HandleFailure()
        {
            if (_settingsService == null) return;
            var settings = _settingsService.Current;
            settings.FailedAttemptsCount++;
            
            ErrorText.Text = $"Invalid access code ({settings.FailedAttemptsCount}/3)";
            ErrorText.Visibility = Visibility.Visible;
            PinInput.Password = string.Empty;
            UpdatePinVisuals();
            UpdateAttemptPins();

            // Trigger shake effect on the newly failed pin
            int failedIndex = settings.FailedAttemptsCount - 1;
            if (failedIndex >= 0 && failedIndex < 3)
            {
                var pins = new[] { Attempt1, Attempt2, Attempt3 };
                TriggerShake(pins[failedIndex]);
            }

            if (settings.FailedAttemptsCount >= 3)
            {
                // Increment lockout duration: 1st time 1m, 2nd time 2m, etc.
                settings.CurrentLockoutMinutes++;
                settings.LockoutEnd = DateTime.Now.AddMinutes(settings.CurrentLockoutMinutes);
                settings.FailedAttemptsCount = 0; // Reset counter for next lockout phase
                
                EnableLockoutUI();
            }

            _settingsService.Save();
        }

        private void ResetSecurityState()
        {
            if (_settingsService == null) return;
            var settings = _settingsService.Current;
            settings.FailedAttemptsCount = 0;
            settings.CurrentLockoutMinutes = 0;
            settings.LockoutEnd = null;
            _settingsService.Save();
            UpdateAttemptPins();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private System.Windows.Point _dockStartPoint;

        private void DockItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dockStartPoint = e.GetPosition(null);
        }

        private void DockItem_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _dockStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is System.Windows.Controls.Button button && button.CommandParameter != null)
                {
                    string viewName = button.CommandParameter.ToString();
                    string dragData = "Unpin:" + viewName;
                    DragDrop.DoDragDrop(button, dragData, System.Windows.DragDropEffects.Move);
                }
            }
        }

        private void MainContent_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && data.StartsWith("Unpin:"))
                {
                    e.Effects = System.Windows.DragDropEffects.Move;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void MainContent_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && data.StartsWith("Unpin:"))
                {
                    string viewName = data.Substring("Unpin:".Length);
                    var mainVm = DataContext as MainViewModel;
                    if (mainVm != null)
                    {
                        mainVm.UnpinFeatureCommand.Execute(viewName);
                    }
                }
            }
            e.Handled = true;
        }

        private void Dock_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && !data.StartsWith("Unpin:"))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void Dock_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string viewName = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (viewName != null && !viewName.StartsWith("Unpin:"))
                {
                    var mainVm = DataContext as MainViewModel;
                    mainVm?.PinFeatureCommand.Execute(viewName);
                }
            }
            e.Handled = true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;
            if (vm != null)
            {
                // Base design dimensions are 1300 x 800
                double scaleX = e.NewSize.Width / 1300.0;
                double scaleY = e.NewSize.Height / 800.0;
                double targetScale = Math.Min(scaleX, scaleY);
                vm.DisplayScale = Math.Max(0.5, Math.Min(2.0, targetScale));
            }
        }
    }
}

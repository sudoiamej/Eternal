using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Eternal.ViewModels;
using Eternal.Services.System;
using Eternal.Models;

namespace Eternal.Views
{
    public partial class NeumorphicMainWindow : Window
    {
        private ISettingsService _settingsService = null!;
        private IScalingService _scalingService = null!;
        private System.Windows.Threading.DispatcherTimer _lockoutTimer = null!;
        private const string OverridePin = "000000";
        private bool _isStartupCompleted = false;
        private bool _isResetting = false;

        private void NeumorphicMainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isStartupCompleted && !_isResetting)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.R)
                {
                    e.Handled = true;
                    ShowFactoryResetPrompt();
                }
            }
        }

        private bool _resetPromptedFromSettings = false;

        public void ShowFactoryResetPromptFromSettings()
        {
            _resetPromptedFromSettings = true;
            
            // Show password prompt directly overlaid on top of current screen
            MainInterfaceGrid.Opacity = 0.3; // Dim main screen slightly
            StartupOverlay.Visibility = Visibility.Visible;
            StartupOverlay.Opacity = 1;
            LockContainer.Visibility = Visibility.Collapsed;
            LoadingContainer.Visibility = Visibility.Collapsed;
            ResetPasscodeBox.Password = "";
            ResetErrorText.Visibility = Visibility.Collapsed;
            
            if (_settingsService != null && !_settingsService.Current.IsStartupLockEnabled)
            {
                // Directly trigger the pending restart flow
                InitiatePendingResetAndRestart();
                return;
            }

            FactoryResetContainer.Visibility = Visibility.Visible;
            ResetPasscodeBox.Focus();
        }

        private void ShowFactoryResetPrompt()
        {
            _resetPromptedFromSettings = false;

            if (_settingsService != null && !_settingsService.Current.IsStartupLockEnabled)
            {
                // Directly trigger the pending restart flow
                InitiatePendingResetAndRestart();
                return;
            }

            LockContainer.Visibility = Visibility.Collapsed;
            LoadingContainer.Visibility = Visibility.Collapsed;
            ResetPasscodeBox.Password = "";
            ResetErrorText.Visibility = Visibility.Collapsed;
            FactoryResetContainer.Visibility = Visibility.Visible;
            ResetPasscodeBox.Focus();
        }

        private void InitiatePendingResetAndRestart()
        {
            if (_settingsService != null)
            {
                _settingsService.Current.IsFactoryResetPending = true;
                _settingsService.Save();
            }

            // Notify the user using CustomNotificationWindow that the app will reset on next launch
            Eternal.Views.Helpers.CustomNotificationWindow.Show(
                "Application data will be fully cleared and reset the next time you start the app.", 
                "Factory Reset", 
                Eternal.Views.Helpers.CustomNotificationWindow.NotificationType.Warning
            );

            // Close the overlay, clean up status, and return back to normal active view
            FactoryResetContainer.Visibility = Visibility.Collapsed;
            
            if (_resetPromptedFromSettings)
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                MainInterfaceGrid.Opacity = 1;
                _isStartupCompleted = true;
            }
            else
            {
                // If it was Ctrl+R on startup, resume where it was (or lock screen)
                var mainVm = DataContext as MainViewModel;
                if (mainVm != null)
                {
                    if (_settingsService != null && _settingsService.Current.IsStartupLockEnabled)
                    {
                        LockContainer.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _ = StartLoadingAndTransition(mainVm);
                    }
                }
            }
        }

        public async Task TriggerFactoryReset()
        {
            _isResetting = true;
            _isStartupCompleted = false;
            
            // 1. Initial Black Screen / Please Wait state
            MainInterfaceGrid.Opacity = 0;
            StartupOverlay.Visibility = Visibility.Visible;
            StartupOverlay.Opacity = 1;
            LockContainer.Visibility = Visibility.Collapsed;
            FactoryResetContainer.Visibility = Visibility.Collapsed;
            LoadingContainer.Visibility = Visibility.Visible;
            LoadingContainer.Opacity = 1;
            TachometerSweep.Width = 0;
            StartupStatusText.Text = "PLEASE WAIT...";
            StartupTipText.Text = "Please wait...";

            // Yield control back to WPF message loop to render the UI updates immediately
            await Task.Delay(100);

            // 2. Perform the actual reset logic:
            if (_settingsService != null)
            {
                try
                {
                    // Reset settings to default
                    var defaults = new AppSettings();
                    _settingsService.Current.RefreshFrequency = defaults.RefreshFrequency;
                    _settingsService.Current.PreloadOnStartup = defaults.PreloadOnStartup;
                    _settingsService.Current.IsAdvancedMode = defaults.IsAdvancedMode;
                    _settingsService.Current.PollingProfile = defaults.PollingProfile;
                    _settingsService.Current.RunAtStartup = defaults.RunAtStartup;
                    _settingsService.Current.MinimizeToTray = defaults.MinimizeToTray;
                    _settingsService.Current.ExportFolderPath = defaults.ExportFolderPath;
                    _settingsService.Current.WmiTimeoutSeconds = defaults.WmiTimeoutSeconds;
                    _settingsService.Current.IsVerboseLoggingEnabled = defaults.IsVerboseLoggingEnabled;
                    _settingsService.Current.ThemeAccentColor = defaults.ThemeAccentColor;
                    _settingsService.Current.FontAdjustmentScale = defaults.FontAdjustmentScale;
                    _settingsService.Current.WindowScale = defaults.WindowScale;
                    
                    _settingsService.Current.IsStartupLockEnabled = defaults.IsStartupLockEnabled;
                    _settingsService.Current.StartupLockPin = defaults.StartupLockPin;
                    _settingsService.Current.LockoutEnd = null;
                    _settingsService.Current.FailedAttemptsCount = 0;
                    _settingsService.Current.CurrentLockoutMinutes = 0;

                    // Also clear and restore default sidebar pinned items
                    _settingsService.Current.PinnedFeatures = new() { "Processes", "Storage", "Dashboard" };
                    
                    // Clear the pending reset flag
                    _settingsService.Current.IsFactoryResetPending = false;

                    // Unregister startup if needed
                    try
                    {
                        string? path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        if (!string.IsNullOrEmpty(path))
                        {
                            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                            key?.DeleteValue("EternalSystemIntelligence", false);
                        }
                    }
                    catch { }

                    // Delete local folders (telemetry, security database, and configuration cache)
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string folder = System.IO.Path.Combine(appData, "EternalAnalytics");
                    if (System.IO.Directory.Exists(folder))
                    {
                        foreach (var file in System.IO.Directory.GetFiles(folder))
                        {
                            try { System.IO.File.Delete(file); } catch { }
                        }
                        foreach (var dir in System.IO.Directory.GetDirectories(folder))
                        {
                            try { System.IO.Directory.Delete(dir, true); } catch { }
                        }
                    }

                    _settingsService.Save();

                    // Apply visual theme, scale defaults, and update sidebar icons
                    var mainVm = DataContext as MainViewModel;
                    if (mainVm != null)
                    {
                        mainVm.ApplyThemeColor();
                        mainVm.UpdateFontScale();
                        mainWinScaleReset();
                        mainVm.InitializeNavigation();
                    }
                }
                catch { }
            }

            // Yield control again before launching sweep
            await Task.Delay(100);

            // 3. Simulated progress bar reset animation
            for (int i = 0; i <= 100; i += 10)
            {
                StartupStatusText.Text = $"CLEARING SYSTEM STORAGE... {i}%";
                TachometerSweep.Width = (i / 100.0) * 300;
                await Task.Delay(100);
            }

            // 4. Black screen again when done saying please wait
            TachometerSweep.Width = 0;
            StartupStatusText.Text = "PLEASE WAIT...";
            StartupTipText.Text = "Please wait...";
            await Task.Delay(800);

            _isResetting = false;
            
            // 5. Finally show the normal startup splash loader
            var currentMainVm = DataContext as MainViewModel;
            if (currentMainVm != null)
            {
                await StartLoadingAndTransition(currentMainVm);
            }
        }

        private void ConfirmReset_Click(object sender, RoutedEventArgs e)
        {
            // Verify against existing passcode (or OverridePin "000000")
            string enteredPin = ResetPasscodeBox.Password;
            string correctPin = _settingsService?.Current?.StartupLockPin ?? OverridePin;

            if (enteredPin != correctPin && enteredPin != OverridePin)
            {
                ResetErrorText.Visibility = Visibility.Visible;
                ResetPasscodeBox.Password = "";
                ResetPasscodeBox.Focus();
                return;
            }

            ResetErrorText.Visibility = Visibility.Collapsed;
            InitiatePendingResetAndRestart();
        }

        private void mainWinScaleReset()
        {
            // Reset window scale immediately to 1.0x (physically 1300x800)
            this.Width = 1300;
            this.Height = 800;
            var mainVm = DataContext as MainViewModel;
            if (mainVm != null)
            {
                mainVm.DisplayScale = 1.0;
            }
        }

        private async void CancelReset_Click(object sender, RoutedEventArgs e)
        {
            FactoryResetContainer.Visibility = Visibility.Collapsed;
            var mainVm = DataContext as MainViewModel;
            if (mainVm != null)
            {
                if (_resetPromptedFromSettings)
                {
                    // Return back to Settings / Main Interface Grid as they were
                    StartupOverlay.Visibility = Visibility.Collapsed;
                    MainInterfaceGrid.Opacity = 1;
                    _isStartupCompleted = true;
                }
                else
                {
                    // Resume the app startup where it left off
                    if (_settingsService != null && _settingsService.Current.IsStartupLockEnabled)
                    {
                        LockContainer.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        await StartLoadingAndTransition(mainVm);
                    }
                }
            }
        }

        public NeumorphicMainWindow()
        {
            InitializeComponent();
            this.Loaded += NeumorphicMainWindow_Loaded;
            this.DpiChanged += (s, e) => {
                if (_scalingService != null)
                {
                    _scalingService.UpdateDpiScale(e.NewDpi.DpiScaleX);
                }
                ApplyDpiScaling();
            };
            this.PreviewKeyDown += NeumorphicMainWindow_PreviewKeyDown;
        }

        private void ApplyDpiScaling()
        {
            try
            {
                if (_scalingService == null)
                {
                    _scalingService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IScalingService>(App.ServiceProvider);
                }

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

                _scalingService.UpdateDpiScale(dpiScale);

                double targetWidth = 1300 * _scalingService.EffectiveUiScale;
                double targetHeight = 800 * _scalingService.EffectiveUiScale;

                // Make sure we fit within the screen work area
                double workingWidth = SystemParameters.WorkArea.Width;
                double workingHeight = SystemParameters.WorkArea.Height;

                if (targetWidth > workingWidth || targetHeight > workingHeight)
                {
                    double ratioX = workingWidth / targetWidth;
                    double ratioY = workingHeight / targetHeight;
                    double limitScale = Math.Min(ratioX, ratioY) * 0.95;
                    this.Width = targetWidth * limitScale;
                    this.Height = targetHeight * limitScale;
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
            // Resolve settings service first so DpiScaling has access to saved settings
            _settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(App.ServiceProvider);
            _scalingService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IScalingService>(App.ServiceProvider);
            _scalingService.ScalingChanged += (s, ev) => ApplyDpiScaling();

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
            _scalingService.Initialize(dpiScale);
            _scalingService.UpdateScales(_settingsService.Current.WindowScale, _settingsService.Current.FontAdjustmentScale);

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

            // Apply initial window scale and font scale to match saved configuration
            var mainVm = DataContext as MainViewModel;
            if (mainVm != null)
            {
                mainVm.UpdateFontScale();
                mainVm.UpdateWindowScale();
            }

            // Intercept and handle pending factory resets immediately on startup
            if (_settingsService != null && _settingsService.Current.IsFactoryResetPending)
            {
                await TriggerFactoryReset();
                return;
            }
            
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
                if (mainVm != null)
                {
                    await StartLoadingAndTransition(mainVm);
                }
            }
        }

        private async Task StartLoadingAndTransition(MainViewModel mainVm)
        {
            bool disableAnimations = 
                Array.Exists(Environment.GetCommandLineArgs(), arg => arg.Equals("--no-animation", StringComparison.OrdinalIgnoreCase) || arg.Equals("--disable-animations", StringComparison.OrdinalIgnoreCase)) ||
                Environment.GetEnvironmentVariable("DISABLE_ANIMATIONS") == "true" ||
                Environment.GetEnvironmentVariable("ANTIGRAVITY") == "true";

            if (disableAnimations)
            {
                LockContainer.Visibility = Visibility.Collapsed;
                LoadingContainer.Visibility = Visibility.Collapsed;
                StartupOverlay.Visibility = Visibility.Collapsed;
                MainInterfaceGrid.Opacity = 1;
                mainVm.StartTimers();
                _ = Task.Run(async () => await mainVm.PreloadAllDataAsync());
                _ = mainVm.Navigate("Home");
                return;
            }

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

            // Set random loading tip
            string[] tips = new[]
            {
                "Tip: Hold Ctrl+Shift+Alt+T to enter developer Unsafe Mode! 🤫",
                "Tip: Debloating Edge telemetry can speed up network request speeds! 🌐",
                "Tip: Unpin items by right-clicking dock shortcuts on the sidebar! 📌",
                "Tip: Trackpad precision scrolling works on all lists and details! 📜",
                "Tip: Unsigned processes running from temporary directories are flagged! 🔍",
                "Tip: Surface scans can inspect raw SMART attributes of disk sectors! 💾",
                "Tip: Toggle between Legacy and Modern views inside the settings panel! ⚙️",
                "Tip: Phi-3 AI engine runs locally without sending data to servers! 🧠",
                "Tip: Safe mode runs logic by disabling aggressive hardware sweeps! 🔋",
                "Tip: Reset DNS and Socket stacks inside the Repair Center section! 🛠️"
            };
            StartupTipText.Text = tips[new Random().Next(tips.Length)];

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
            
            fadeOutOverlay.Completed += (s, ev) => {
                StartupOverlay.Visibility = Visibility.Collapsed;
                _isStartupCompleted = true;
            };

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

        private void LockoutTimer_Tick(object? sender, EventArgs e)
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
            var confirmWin = new Eternal.Views.Helpers.ExitConfirmWindow();
            confirmWin.Owner = this;
            if (confirmWin.ShowDialog() == true)
            {
                this.Close();
            }
        }

        private void HeaderSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var mainVm = DataContext as MainViewModel;
            if (mainVm?.CommandPaletteVm != null)
            {
                mainVm.CommandPaletteVm.Open();
            }
        }

        private void HeaderSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var mainVm = DataContext as MainViewModel;
            if (mainVm?.CommandPaletteVm != null)
            {
                // Accessing the private search method indirectly by setting property to trigger community toolkit's OnSearchTextChanged
                mainVm.CommandPaletteVm.SearchText = HeaderSearchBox.Text;
                if (!mainVm.CommandPaletteVm.IsOpen && !string.IsNullOrEmpty(HeaderSearchBox.Text))
                {
                    mainVm.CommandPaletteVm.IsOpen = true;
                }
            }
        }

        private void SearchListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox && listBox.SelectedItem != null)
            {
                var mainVm = DataContext as MainViewModel;
                if (mainVm?.CommandPaletteVm != null)
                {
                    var selected = listBox.SelectedItem as ViewModels.Modules.CommandItem;
                    if (selected != null)
                    {
                        mainVm.CommandPaletteVm.ExecuteSelected(selected);
                        HeaderSearchBox.Text = string.Empty;
                    }
                }
                listBox.SelectedItem = null;
            }
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
                    string? viewName = button.CommandParameter.ToString();
                    if (viewName != null)
                    {
                        string dragData = "Unpin:" + viewName;
                        DragDrop.DoDragDrop(button, dragData, System.Windows.DragDropEffects.Move);
                    }
                }
            }
        }

        private void MainContent_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string? data = e.Data.GetData(System.Windows.DataFormats.StringFormat) as string;
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

        private bool _isPermanentlyExpanded = false;

        private void LogoButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                _isPermanentlyExpanded = !_isPermanentlyExpanded;
                vm.IsSidebarExpanded = _isPermanentlyExpanded;
            }
            e.Handled = true;
        }

        private void SidebarBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Hover-to-expand disabled for now
        }

        private void SidebarBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Hover-to-expand disabled for now
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Manual DPI adjustment is controlled directly via settings.
        }
    }
}

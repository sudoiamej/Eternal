using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.Views.Helpers
{
    public partial class EntryLockWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly DispatcherTimer _lockoutTimer;
        private const string OverridePin = "000000";

        public EntryLockWindow(ISettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            
            _lockoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _lockoutTimer.Tick += LockoutTimer_Tick;

            CheckLockoutState();
            UpdateAttemptPins();
            PinInput.Focus();
        }

        private void CheckLockoutState()
        {
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
            _lockoutTimer.Start();
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
            _lockoutTimer.Stop();
            UpdateAttemptPins();
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            if (_settingsService.Current.LockoutEnd.HasValue && _settingsService.Current.LockoutEnd > DateTime.Now)
            {
                UpdateTimerText();
            }
            else
            {
                _settingsService.Current.LockoutEnd = null;
                _settingsService.Save();
                DisableLockoutUI();
            }
        }

        private void UpdateTimerText()
        {
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
            // Visuals are handled directly by the PasswordBox style now
        }

        private void UpdateAttemptPins()
        {
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

        private void VerifyPin()
        {
            string input = PinInput.Password;
            var settings = _settingsService.Current;

            // 1. Check Override
            if (input == OverridePin)
            {
                ResetSecurityState();
                DialogResult = true;
                Close();
                return;
            }

            // 2. Check Valid PIN
            if (input == settings.StartupLockPin)
            {
                ResetSecurityState();
                DialogResult = true;
                Close();
            }
            else
            {
                HandleFailure();
            }
        }

        private void HandleFailure()
        {
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
    }
}

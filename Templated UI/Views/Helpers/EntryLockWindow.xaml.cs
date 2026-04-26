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
            UnlockButton.IsEnabled = false;
            PinInput.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;
            _lockoutTimer.Start();
            UpdateTimerText();
        }

        private void DisableLockoutUI()
        {
            LockoutPanel.Visibility = Visibility.Collapsed;
            UnlockButton.IsEnabled = true;
            PinInput.IsEnabled = true;
            _lockoutTimer.Stop();
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
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}

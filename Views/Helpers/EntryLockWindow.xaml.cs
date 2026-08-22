using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Security;
using Eternal.Services.System;

namespace Eternal.Views.Helpers
{
    public partial class EntryLockWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private readonly ISecurityService? _securityService;

        public EntryLockWindow(ISettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _securityService = App.ServiceProvider?.GetService<ISecurityService>();

            _ = InitializeWindowsHelloAsync();
        }

        private async Task InitializeWindowsHelloAsync()
        {
            await Task.Delay(400); // Smooth entrance animation delay

            if (_securityService != null)
            {
                bool isAvailable = await _securityService.IsWindowsHelloAvailableAsync();
                if (!isAvailable)
                {
                    StatusSubtext.Text = "Windows Hello System PIN is not configured on this device.";
                    ErrorText.Text = "Windows Hello unavailable. System security bypassed for local session.";
                    ErrorText.Visibility = Visibility.Visible;
                }
                else
                {
                    // Auto-trigger Windows Hello System OS Prompt on load
                    await TriggerWindowsHelloAuthAsync();
                }
            }
        }

        private async Task TriggerWindowsHelloAuthAsync()
        {
            if (_securityService == null) return;

            WindowsHelloButton.IsEnabled = false;
            ErrorText.Visibility = Visibility.Collapsed;
            StatusSubtext.Text = "Prompting Windows Hello System Security...";

            bool authenticated = await _securityService.AuthenticateWithWindowsHelloAsync("Authenticate to access Eternal Workstation");

            if (authenticated)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                WindowsHelloButton.IsEnabled = true;
                StatusSubtext.Text = "Windows Hello verification failed or was cancelled.";
                ErrorText.Text = "Authentication Failed. Click below to retry Windows Hello PIN.";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void WindowsHello_Click(object sender, RoutedEventArgs e)
        {
            await TriggerWindowsHelloAuthAsync();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}

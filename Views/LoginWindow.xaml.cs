using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Security;

namespace Eternal.Views
{
    public partial class LoginWindow : Window
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;

        private readonly ISecurityService? _securityService;

        public LoginWindow()
        {
            InitializeComponent();

            UsernameTextBlock.Text = Environment.UserName;

            try
            {
                _securityService = App.ServiceProvider?.GetService<ISecurityService>();
            }
            catch { }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void WindowsHelloButton_Click(object sender, RoutedEventArgs e)
        {
            await AttemptWindowsHelloAsync();
        }

        private async Task AttemptWindowsHelloAsync()
        {
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            StatusTextBlock.Text = "Verifying identity with Windows Hello...";

            try
            {
                if (_securityService != null)
                {
                    bool isHelloAvailable = await _securityService.IsWindowsHelloAvailableAsync();
                    if (isHelloAvailable)
                    {
                        bool verified = await _securityService.AuthenticateWithWindowsHelloAsync("Authenticate to unlock Eternal System Intelligence");
                        if (verified)
                        {
                            GrantAccess();
                            return;
                        }
                    }
                }
            }
            catch { }

            StatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
            StatusTextBlock.Text = "Windows Hello PIN skipped or unavailable. Enter Windows password below.";
        }

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        private void PasswordInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ValidatePassword();
            }
        }

        private void PasswordInputBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            StatusTextBlock.Text = "Identity verification required to proceed.";
        }

        private void ValidatePassword()
        {
            string password = PasswordInputBox.Password;
            string username = Environment.UserName;
            string domain = Environment.UserDomainName;

            IntPtr token = IntPtr.Zero;
            bool isValid = LogonUser(username, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out token);

            if (isValid)
            {
                if (token != IntPtr.Zero) CloseHandle(token);
                GrantAccess();
                return;
            }

            // If empty password failed with LogonUser, check if Windows Hello can authenticate or if password is strictly required
            if (string.IsNullOrEmpty(password))
            {
                StatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
                StatusTextBlock.Text = "Windows account requires a password or Windows Hello PIN. Please enter your password.";
            }
            else
            {
                StatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("CriticalBrush");
                StatusTextBlock.Text = "Incorrect Windows password. Please try again.";
            }
        }

        private void EmergencyRecoveryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Fallback 1: Active Windows Desktop Session Verification
                var identity = WindowsIdentity.GetCurrent();
                if (identity.IsAuthenticated && !string.IsNullOrEmpty(identity.Name))
                {
                    GrantAccess();
                    return;
                }

                // Fallback 2: WinPE Recovery Environment Auto-Bypass
                if (Eternal.Helpers.OsHelper.IsWinPE())
                {
                    GrantAccess();
                    return;
                }
            }
            catch { }

            StatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("CriticalBrush");
            StatusTextBlock.Text = "Emergency recovery failed. Active Windows identity token not found.";
        }

        private void GrantAccess()
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

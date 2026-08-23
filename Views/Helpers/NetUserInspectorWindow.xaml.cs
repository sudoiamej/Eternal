using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Eternal.Views.Helpers
{
    public partial class NetUserInspectorWindow : Window
    {
        private string _rawOutput = "";

        public NetUserInspectorWindow()
        {
            InitializeComponent();
            _ = LoadNetUserDataAsync();
        }

        private async Task LoadNetUserDataAsync()
        {
            string username = Environment.UserName;
            HeaderUsernameText.Text = $"NET USER: {username.ToUpper()}";
            UsernameTitleText.Text = username;

            try
            {
                if (System.Windows.Application.Current.MainWindow?.DataContext is ViewModels.MainViewModel vm && vm.HasUserProfileImage && vm.UserProfileImage != null)
                {
                    UserAccountImageBrush.ImageSource = vm.UserProfileImage;
                    UserAccountPictureEllipse.Visibility = Visibility.Visible;
                }
            }
            catch { }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = $"user \"{username}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                _rawOutput = output;
                RawOutputTextBox.Text = output;

                ParseNetUserOutput(output);
            }
            catch (Exception ex)
            {
                RawOutputTextBox.Text = $"Failed to execute net user: {ex.Message}";
            }
        }

        private void ParseNetUserOutput(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var groups = new List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("Full Name", StringComparison.OrdinalIgnoreCase))
                {
                    string val = GetValueAfterHeader(line, "Full Name");
                    FullNameText.Text = string.IsNullOrWhiteSpace(val) ? $"Full Name: {Environment.UserName}" : $"Full Name: {val}";
                }
                else if (line.StartsWith("Account active", StringComparison.OrdinalIgnoreCase))
                {
                    string val = GetValueAfterHeader(line, "Account active");
                    bool isActive = val.StartsWith("Yes", StringComparison.OrdinalIgnoreCase);
                    AccountStatusText.Text = isActive ? "ACCOUNT ACTIVE" : "ACCOUNT DISABLED";
                    AccountStatusBorder.Background = new SolidColorBrush(isActive ? System.Windows.Media.Color.FromRgb(16, 32, 20) : System.Windows.Media.Color.FromRgb(32, 16, 16));
                    AccountStatusBorder.BorderBrush = new SolidColorBrush(isActive ? System.Windows.Media.Color.FromRgb(34, 197, 94) : System.Windows.Media.Color.FromRgb(239, 68, 68));
                    AccountStatusText.Foreground = new SolidColorBrush(isActive ? System.Windows.Media.Color.FromRgb(34, 197, 94) : System.Windows.Media.Color.FromRgb(239, 68, 68));
                }
                else if (line.StartsWith("Password last set", StringComparison.OrdinalIgnoreCase))
                {
                    PasswordLastSetText.Text = GetValueAfterHeader(line, "Password last set");
                }
                else if (line.StartsWith("Password expires", StringComparison.OrdinalIgnoreCase))
                {
                    PasswordExpiresText.Text = GetValueAfterHeader(line, "Password expires");
                }
                else if (line.StartsWith("Last logon", StringComparison.OrdinalIgnoreCase))
                {
                    LastLogonText.Text = GetValueAfterHeader(line, "Last logon");
                }
                else if (line.StartsWith("Password required", StringComparison.OrdinalIgnoreCase))
                {
                    PasswordRequiredText.Text = GetValueAfterHeader(line, "Password required");
                }
                else if (line.StartsWith("Logon hours allowed", StringComparison.OrdinalIgnoreCase))
                {
                    LogonHoursText.Text = $"Logon Hours: {GetValueAfterHeader(line, "Logon hours allowed")}";
                }
                else if (line.StartsWith("Local Group Memberships", StringComparison.OrdinalIgnoreCase))
                {
                    string val = GetValueAfterHeader(line, "Local Group Memberships");
                    var parsedGroups = val.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var g in parsedGroups)
                    {
                        string clean = g.TrimStart('*').Trim();
                        if (!string.IsNullOrEmpty(clean)) groups.Add(clean);
                    }
                }
            }

            if (!groups.Any())
            {
                groups.Add("Users");
            }

            GroupsItemsControl.ItemsSource = groups;
        }

        private string GetValueAfterHeader(string line, string header)
        {
            if (line.StartsWith(header, StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(header.Length).Trim(' ', '\t', ':');
            }
            return "";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CopyRawOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_rawOutput))
            {
                System.Windows.Clipboard.SetText(_rawOutput);
                System.Windows.MessageBox.Show("Raw net user output copied to clipboard!", "Eternal Telemetry", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

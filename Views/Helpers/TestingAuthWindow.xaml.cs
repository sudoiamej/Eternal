using System.Windows;
using System.Windows.Input;
using Eternal.Models;

namespace Eternal.Views.Helpers
{
    public partial class TestingAuthWindow : Window
    {
        public TestingAuthWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                PinInput.Focus();
                Keyboard.Focus(PinInput);
            };
        }

        private async void Authorize_Click(object sender, RoutedEventArgs e)
        {
            if (PinInput.Password == DeveloperEnvironment.DevAccessPin)
            {
                ConsoleStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 134, 255));
                ConsoleStatusText.Text = "SYS:\\> success: signature_match. unlocking system modules...";
                PinInput.IsEnabled = false;
                await Task.Delay(800); // Give user a brief moment to see success feedback
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ConsoleStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 58, 58));
                ConsoleStatusText.Text = "SYS:\\> error: access_denied. cryptographic signature invalid.";
                PinInput.Clear();
                
                // Spring shake effect
                var oldMargin = this.Margin;
                for (int i = 0; i < 2; i++)
                {
                    this.Left -= 10; await Task.Delay(40);
                    this.Left += 20; await Task.Delay(40);
                    this.Left -= 10; await Task.Delay(40);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void PinInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Authorize_Click(sender, e);
            }
        }

        private void PinInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ConsoleStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 134, 255));
            ConsoleStatusText.Text = $"SYS:\\> status: crypt_key_buffering [length: {PinInput.Password.Length}]";
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}

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

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {
            if (PinInput.Password == DeveloperEnvironment.DevAccessPin)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                PinInput.Clear();
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}

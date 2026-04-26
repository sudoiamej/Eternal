using System.Windows;

namespace Eternal.Views.Helpers
{
    public partial class StartupPinEditWindow : Window
    {
        public string NewPin { get; private set; } = string.Empty;

        public StartupPinEditWindow()
        {
            InitializeComponent();
            NewPinInput.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string pin1 = NewPinInput.Password;
            string pin2 = ConfirmPinInput.Password;

            if (pin1.Length == 6 && pin1 == pin2 && int.TryParse(pin1, out _))
            {
                NewPin = pin1;
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                NewPinInput.Password = string.Empty;
                ConfirmPinInput.Password = string.Empty;
                NewPinInput.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

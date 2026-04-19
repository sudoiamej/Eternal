using System;
using System.Windows;
using System.Windows.Input;
using Eternal.Models;

namespace Eternal.Views.Helpers
{
    public partial class TestingAuthWindow : Window
    {
        public bool IsAuthorized { get; private set; }

        public TestingAuthWindow()
        {
            InitializeComponent();
            PinInput.Focus();
        }

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {
            VerifyPin();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
            if (PinInput.Password == DeveloperEnvironment.DevAccessPin)
            {
                IsAuthorized = true;
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
                PinInput.Password = string.Empty;
            }
        }
    }
}

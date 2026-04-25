using System.Windows;
using Eternal.Services.System;

namespace Eternal.Views.Helpers
{
    public partial class CreateUserWindow : Window
    {
        private readonly IUserGroupService _userService;

        public CreateUserWindow(IUserGroupService userService)
        {
            InitializeComponent();
            _userService = userService;
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter a username.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (PassBox.Password != ConfirmPassBox.Password)
            {
                System.Windows.MessageBox.Show("Passwords do not match.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool success = await _userService.CreateUserAsync(
                UsernameBox.Text,
                PassBox.Password,
                FullNameBox.Text,
                DescriptionBox.Text,
                MustChangeCheck.IsChecked ?? false,
                CannotChangeCheck.IsChecked ?? false,
                NeverExpiresCheck.IsChecked ?? false,
                DisabledCheck.IsChecked ?? false
            );

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to create user. Ensure you have administrative privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

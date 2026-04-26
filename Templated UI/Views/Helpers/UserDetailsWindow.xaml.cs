using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.Views.Helpers
{
    public partial class UserDetailsWindow : Window
    {
        private readonly IUserGroupService _userService;
        private readonly UserAccount _user;

        public UserDetailsWindow(IUserGroupService userService, UserAccount user)
        {
            InitializeComponent();
            _userService = userService;
            _user = user;

            PopulateData();
        }

        private void PopulateData()
        {
            UsernameTitle.Text = _user.Name;
            SidTitle.Text = $"SID: {_user.Sid}";
            FullNameBox.Text = _user.FullName;
            DescriptionBox.Text = _user.Description;
            MustChangeCheck.IsChecked = _user.MustChangePasswordAtNextLogon;
            CannotChangeCheck.IsChecked = _user.UserCannotChangePassword;
            NeverExpiresCheck.IsChecked = _user.PasswordNeverExpires;
            DisabledCheck.IsChecked = !_user.IsEnabled;
            LockedCheck.IsChecked = _user.IsLockedOut;

            if (_user.IsLockedOut)
            {
                UnlockButton.Visibility = Visibility.Visible;
            }

            foreach (var g in _user.Groups)
            {
                GroupsList.Items.Add(g);
            }
        }

        private async void Unlock_Click(object sender, RoutedEventArgs e)
        {
            bool success = await _userService.UnlockUserAccountAsync(_user.Name);
            if (success)
            {
                LockedCheck.IsChecked = false;
                UnlockButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            // Simplified: Add to a hardcoded group or show a simple input
            string? groupName = InputWindow.Show("Enter group name to add user to:", "Add to Group", "Administrators");
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                bool success = await _userService.AddUserToGroupAsync(_user.Name, groupName);
                if (success && !GroupsList.Items.Contains(groupName))
                    GroupsList.Items.Add(groupName);
            }
        }

        private async void RemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupsList.SelectedItem is string groupName)
            {
                bool success = await _userService.RemoveUserFromGroupAsync(_user.Name, groupName);
                if (success)
                    GroupsList.Items.Remove(groupName);
            }
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            _user.FullName = FullNameBox.Text;
            _user.Description = DescriptionBox.Text;
            _user.MustChangePasswordAtNextLogon = MustChangeCheck.IsChecked ?? false;
            _user.UserCannotChangePassword = CannotChangeCheck.IsChecked ?? false;
            _user.PasswordNeverExpires = NeverExpiresCheck.IsChecked ?? false;
            _user.IsEnabled = !(DisabledCheck.IsChecked ?? false);

            bool success = await _userService.UpdateUserPropertiesAsync(_user.Name, _user);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to update user properties.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

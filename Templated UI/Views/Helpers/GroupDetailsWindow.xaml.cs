using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.Views.Helpers
{
    public partial class GroupDetailsWindow : Window
    {
        private readonly IUserGroupService _userService;
        private readonly UserGroup _group;

        public GroupDetailsWindow(IUserGroupService userService, UserGroup group)
        {
            InitializeComponent();
            _userService = userService;
            _group = group;

            PopulateData();
        }

        private void PopulateData()
        {
            GroupNameTitle.Text = _group.Name;
            DescriptionTitle.Text = _group.Description;

            foreach (var m in _group.Members)
            {
                MembersList.Items.Add(m);
            }
        }

        private async void AddMember_Click(object sender, RoutedEventArgs e)
        {
            string? username = InputWindow.Show("Enter username to add to group:", "Add Member", "");
            if (!string.IsNullOrWhiteSpace(username))
            {
                bool success = await _userService.AddUserToGroupAsync(username, _group.Name);
                if (success && !MembersList.Items.Contains(username))
                    MembersList.Items.Add(username);
            }
        }

        private async void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (MembersList.SelectedItem is string username)
            {
                bool success = await _userService.RemoveUserFromGroupAsync(username, _group.Name);
                if (success)
                    MembersList.Items.Remove(username);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

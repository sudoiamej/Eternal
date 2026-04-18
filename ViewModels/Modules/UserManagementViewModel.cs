using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly IUserGroupService _userGroupService;

        public ObservableCollection<UserAccount> Users { get; } = new ObservableCollection<UserAccount>();
        public ObservableCollection<UserGroup> Groups { get; } = new ObservableCollection<UserGroup>();

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private UserAccount? _selectedUser;
        [ObservableProperty] private UserGroup? _selectedGroup;

        public UserManagementViewModel(IUserGroupService userGroupService)
        {
            _userGroupService = userGroupService;
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        }

        public IAsyncRelayCommand LoadDataCommand { get; }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            StatusMessage = "Fetching users and groups...";
            try
            {
                var usersTask = _userGroupService.GetUsersAsync();
                var groupsTask = _userGroupService.GetGroupsAsync();

                await Task.WhenAll(usersTask, groupsTask);

                Users.Clear();
                foreach (var u in usersTask.Result) Users.Add(u);

                Groups.Clear();
                foreach (var g in groupsTask.Result) Groups.Add(g);

                StatusMessage = $"Loaded {Users.Count} users and {Groups.Count} groups.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task ToggleUserStatus(UserAccount user)
        {
            if (user == null) return;
            bool newStatus = !user.IsEnabled;
            StatusMessage = $"{(newStatus ? "Enabling" : "Disabling")} user {user.Name}...";
            
            if (await _userGroupService.SetUserEnabledAsync(user.Name, newStatus))
            {
                user.IsEnabled = newStatus;
                StatusMessage = $"User {user.Name} {(newStatus ? "enabled" : "disabled")}.";
                await LoadDataAsync();
            }
            else
            {
                StatusMessage = "Failed to update user status. Admin rights required.";
            }
        }

        [RelayCommand]
        private async Task DeleteUser(UserAccount user)
        {
            if (user == null) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete user '{user.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            StatusMessage = $"Deleting user {user.Name}...";
            if (await _userGroupService.DeleteUserAsync(user.Name))
            {
                StatusMessage = $"User {user.Name} deleted.";
                await LoadDataAsync();
            }
            else
            {
                StatusMessage = "Failed to delete user.";
            }
        }

        [RelayCommand]
        private async Task DeleteGroup(UserGroup group)
        {
            if (group == null) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete group '{group.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            StatusMessage = $"Deleting group {group.Name}...";
            if (await _userGroupService.DeleteGroupAsync(group.Name))
            {
                StatusMessage = $"Group {group.Name} deleted.";
                await LoadDataAsync();
            }
            else
            {
                StatusMessage = "Failed to delete group.";
            }
        }
    }
}

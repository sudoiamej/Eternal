using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.Views.Helpers;
using System.Windows;

namespace Eternal.ViewModels.Modules
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserGroupService _userGroupService;
        private readonly IToastService _toastService;

        public ObservableCollection<UserAccount> Users { get; } = new ObservableCollection<UserAccount>();
        public ObservableCollection<UserGroup> Groups { get; } = new ObservableCollection<UserGroup>();

        [ObservableProperty] private UserAccount? _selectedUser;
        [ObservableProperty] private UserGroup? _selectedGroup;

        [ObservableProperty] private string _passwordAgeString = "N/A";
        [ObservableProperty] private string _lastLogonString = "N/A";
        [ObservableProperty] private bool _isLockedOut;
        [ObservableProperty] private bool _passwordNeverExpires;

        [ObservableProperty] private bool _isInAdministratorsGroup;
        [ObservableProperty] private bool _isInUsersGroup;
        [ObservableProperty] private bool _isInRemoteDesktopUsersGroup;

        private bool _updatingGroupsSilently = false;

        partial void OnSelectedUserChanged(UserAccount? oldValue, UserAccount? newValue)
        {
            if (newValue != null)
            {
                IsLockedOut = newValue.IsLockedOut;
                PasswordNeverExpires = newValue.PasswordNeverExpires;
                
                if (newValue.PasswordLastSet.HasValue)
                {
                    var age = DateTime.Now - newValue.PasswordLastSet.Value;
                    PasswordAgeString = $"{age.Days} days (Set: {newValue.PasswordLastSet.Value:g})";
                }
                else
                {
                    PasswordAgeString = "Never Changed / Unknown";
                }

                LastLogonString = newValue.LastLogon.HasValue 
                    ? newValue.LastLogon.Value.ToString("g") 
                    : "Never";

                // Update group memberships
                _updatingGroupsSilently = true;
                IsInAdministratorsGroup = newValue.Groups.Contains("Administrators", StringComparer.OrdinalIgnoreCase);
                IsInUsersGroup = newValue.Groups.Contains("Users", StringComparer.OrdinalIgnoreCase);
                IsInRemoteDesktopUsersGroup = newValue.Groups.Contains("Remote Desktop Users", StringComparer.OrdinalIgnoreCase);
                _updatingGroupsSilently = false;
            }
            else
            {
                IsLockedOut = false;
                PasswordNeverExpires = false;
                PasswordAgeString = "N/A";
                LastLogonString = "N/A";
                IsInAdministratorsGroup = false;
                IsInUsersGroup = false;
                IsInRemoteDesktopUsersGroup = false;
            }
        }

        partial void OnIsInAdministratorsGroupChanged(bool value)
        {
            if (_updatingGroupsSilently || SelectedUser == null) return;
            _ = HandleGroupMembershipChangeAsync("Administrators", value);
        }

        partial void OnIsInUsersGroupChanged(bool value)
        {
            if (_updatingGroupsSilently || SelectedUser == null) return;
            _ = HandleGroupMembershipChangeAsync("Users", value);
        }

        partial void OnIsInRemoteDesktopUsersGroupChanged(bool value)
        {
            if (_updatingGroupsSilently || SelectedUser == null) return;
            _ = HandleGroupMembershipChangeAsync("Remote Desktop Users", value);
        }

        private async Task HandleGroupMembershipChangeAsync(string groupName, bool add)
        {
            if (SelectedUser == null) return;

            if (string.Equals(groupName, "Administrators", StringComparison.OrdinalIgnoreCase) && 
                !add && 
                string.Equals(SelectedUser.Name, Environment.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var result = System.Windows.MessageBox.Show(
                    "WARNING: You are about to remove your own user account from the Administrators group. This may lock you out of administrative features. Do you want to continue?",
                    "Security Safeguard",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    _updatingGroupsSilently = true;
                    IsInAdministratorsGroup = true;
                    _updatingGroupsSilently = false;
                    return;
                }
            }

            await ExecuteBusyActionAsync(async () =>
            {
                bool success;
                if (add)
                {
                    success = await _userGroupService.AddUserToGroupAsync(SelectedUser.Name, groupName);
                }
                else
                {
                    success = await _userGroupService.RemoveUserFromGroupAsync(SelectedUser.Name, groupName);
                }

                if (success)
                {
                    _toastService.ShowSuccess($"User group membership updated: {groupName}");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError($"Failed to update group membership: {groupName}");
                    // Revert UI checkbox
                    _updatingGroupsSilently = true;
                    if (string.Equals(groupName, "Administrators", StringComparison.OrdinalIgnoreCase))
                        IsInAdministratorsGroup = !add;
                    else if (string.Equals(groupName, "Users", StringComparison.OrdinalIgnoreCase))
                        IsInUsersGroup = !add;
                    else if (string.Equals(groupName, "Remote Desktop Users", StringComparison.OrdinalIgnoreCase))
                        IsInRemoteDesktopUsersGroup = !add;
                    _updatingGroupsSilently = false;
                }
            }, "Updating Group Membership...");
        }

        [RelayCommand]
        private async Task UnlockAccountAsync()
        {
            if (SelectedUser == null) return;
            await ExecuteBusyActionAsync(async () =>
            {
                if (await _userGroupService.UnlockUserAccountAsync(SelectedUser.Name))
                {
                    IsLockedOut = false;
                    _toastService.ShowSuccess($"Account {SelectedUser.Name} unlocked successfully.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to unlock account.");
                }
            }, "Unlocking Account...");
        }

        [RelayCommand]
        private async Task TogglePasswordNeverExpiresAsync()
        {
            if (SelectedUser == null) return;
            bool newValue = !SelectedUser.PasswordNeverExpires;
            await ExecuteBusyActionAsync(async () =>
            {
                var props = SelectedUser;
                props.PasswordNeverExpires = newValue;
                if (await _userGroupService.UpdateUserPropertiesAsync(SelectedUser.Name, props))
                {
                    PasswordNeverExpires = newValue;
                    _toastService.ShowSuccess($"Password Never Expires policy set to {newValue} for {SelectedUser.Name}.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to update password policy.");
                }
            }, "Updating Password Policy...");
        }

        public UserManagementViewModel(IUserGroupService userGroupService, IToastService toastService)
        {
            _userGroupService = userGroupService;
            _toastService = toastService;
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        }

        public IAsyncRelayCommand LoadDataCommand { get; }

        public async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var usersTask = _userGroupService.GetUsersAsync();
                var groupsTask = _userGroupService.GetGroupsAsync();

                await Task.WhenAll(usersTask, groupsTask);

                Users.Clear();
                foreach (var u in usersTask.Result) Users.Add(u);

                Groups.Clear();
                foreach (var g in groupsTask.Result) Groups.Add(g);
            }, "Syncing User Accounts...");
        }

        [RelayCommand]
        private void CreateUser()
        {
            var win = new CreateUserWindow(_userGroupService);
            win.Owner = System.Windows.Application.Current.MainWindow;
            if (win.ShowDialog() == true)
            {
                _toastService.ShowSuccess("User created successfully.");
                _ = LoadDataAsync();
            }
        }

        [RelayCommand]
        private void ShowUserDetails(UserAccount user)
        {
            if (user == null) return;
            var win = new UserDetailsWindow(_userGroupService, user);
            win.Owner = System.Windows.Application.Current.MainWindow;
            if (win.ShowDialog() == true)
            {
                _toastService.ShowInfo($"Properties updated for {user.Name}");
                _ = LoadDataAsync();
            }
        }

        [RelayCommand]
        private void ShowGroupDetails(UserGroup group)
        {
            if (group == null) return;
            var win = new GroupDetailsWindow(_userGroupService, group);
            win.Owner = System.Windows.Application.Current.MainWindow;
            if (win.ShowDialog() == true)
            {
                _toastService.ShowInfo($"Members updated for {group.Name}");
                _ = LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task ToggleUserStatus(UserAccount user)
        {
            if (user == null) return;
            bool newStatus = !user.IsEnabled;
            
            await ExecuteBusyActionAsync(async () =>
            {
                if (await _userGroupService.SetUserEnabledAsync(user.Name, newStatus))
                {
                    user.IsEnabled = newStatus;
                    _toastService.ShowInfo($"User {user.Name} {(newStatus ? "enabled" : "disabled")}.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to update status. Admin rights required.");
                }
            }, "Updating Account State...");
        }

        [RelayCommand]
        private async Task DeleteUser(UserAccount user)
        {
            if (user == null) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete user '{user.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            await ExecuteBusyActionAsync(async () =>
            {
                if (await _userGroupService.DeleteUserAsync(user.Name))
                {
                    _toastService.ShowSuccess($"User {user.Name} deleted.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to delete user.");
                }
            }, "Deleting Account...");
        }

        [RelayCommand]
        private async Task DeleteGroup(UserGroup group)
        {
            if (group == null) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete group '{group.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            await ExecuteBusyActionAsync(async () =>
            {
                if (await _userGroupService.DeleteGroupAsync(group.Name))
                {
                    _toastService.ShowSuccess($"Group {group.Name} deleted.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to delete group.");
                }
            }, "Deleting Group...");
        }
    }
}

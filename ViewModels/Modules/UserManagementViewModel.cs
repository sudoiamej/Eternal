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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;
using Eternal.Views.Helpers;

namespace Eternal.ViewModels.Modules
{
    public partial class EnvironmentViewModel : BaseViewModel
    {
        private readonly IEnvironmentService _envService;

        public ObservableCollection<EnvVar> UserVariables { get; } = new();
        public ObservableCollection<EnvVar> SystemVariables { get; } = new();

        public EnvironmentViewModel(IEnvironmentService envService)
        {
            _envService = envService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try 
            { 
                var allVars = await _envService.GetVariablesAsync(); 
                
                UserVariables.Clear();
                SystemVariables.Clear();

                foreach (var v in allVars.Where(x => !x.IsSystem).OrderBy(x => x.Name))
                    UserVariables.Add(v);

                foreach (var v in allVars.Where(x => x.IsSystem).OrderBy(x => x.Name))
                    SystemVariables.Add(v);
            } 
            catch 
            { 
                UserVariables.Clear();
                SystemVariables.Clear();
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task AddVariable()
        {
            var dialog = new EnvVarEditWindow();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    bool success = await _envService.SetVariableAsync(dialog.VarName, dialog.VarValue, dialog.IsSystem);
                    if (success) await LoadDataAsync();
                }, "Provisioning Variable...");
            }
        }

        [RelayCommand]
        private async Task UpdateVariable(EnvVar var)
        {
            if (var == null) return;
            var dialog = new EnvVarEditWindow(var.Name, var.Value, var.IsSystem, true);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    bool success = await _envService.SetVariableAsync(var.Name, dialog.VarValue, var.IsSystem);
                    if (success) await LoadDataAsync();
                }, "Committing Changes...");
            }
        }

        [RelayCommand]
        private async Task DeleteVariable(EnvVar var)
        {
            if (var == null) return;
            var confirm = System.Windows.MessageBox.Show($"Permanently delete environment variable '{var.Name}'?", "System Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    bool success = await _envService.SetVariableAsync(var.Name, "", var.IsSystem);
                    if (success) await LoadDataAsync();
                }, "Purging Variable...");
            }
        }
    }
}

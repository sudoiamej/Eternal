using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class EnvironmentViewModel : ObservableObject
    {
        private readonly IEnvironmentService _envService;

        [ObservableProperty] private List<EnvVar> _variables = new();
        [ObservableProperty] private bool _isLoading;

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
            try { Variables = await _envService.GetVariablesAsync(); } 
            catch { Variables = new List<EnvVar>(); }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task UpdateVariable(EnvVar var)
        {
            // Simplified UI logic for prompt
            System.Windows.MessageBox.Show($"Editing {var.Name}. Feature coming in full build.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
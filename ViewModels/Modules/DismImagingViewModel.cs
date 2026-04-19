using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class DismImagingViewModel : ObservableObject
    {
        private readonly IDismService _dismService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private string _selectedFilePath = string.Empty;
        [ObservableProperty] private WimFileDetails? _wimDetails;
        [ObservableProperty] private bool _isLoading;

        public DismImagingViewModel(IDismService dismService, ILoggingService loggingService)
        {
            _dismService = dismService;
            _loggingService = loggingService;
        }

        [RelayCommand]
        private async Task BrowseImageAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Windows Image File",
                Filter = "Windows Image (*.wim;*.esd;*.swm)|*.wim;*.esd;*.swm|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                await LoadImageInfoAsync();
            }
        }

        [RelayCommand]
        private async Task LoadImageInfoAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;

            IsLoading = true;
            _loggingService.Log($"DISM: Analyzing image file {SelectedFilePath}");
            
            try
            {
                WimDetails = await _dismService.GetImageInfoAsync(SelectedFilePath);
                if (WimDetails == null || WimDetails.Images.Count == 0)
                {
                    System.Windows.MessageBox.Show("Failed to read image information. Ensure the file is a valid .wim or .esd file.", "DISM Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Log($"DISM Error: {ex.Message}");
                System.Windows.MessageBox.Show($"Error reading image: {ex.Message}", "DISM Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Clear()
        {
            SelectedFilePath = string.Empty;
            WimDetails = null;
        }

        [RelayCommand]
        private void CopyPath()
        {
            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                System.Windows.Clipboard.SetText(SelectedFilePath);
            }
        }
    }
}

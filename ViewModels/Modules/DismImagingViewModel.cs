using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class DismImagingViewModel : BaseViewModel
    {
        private readonly IDismService _dismService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private string _selectedFilePath = string.Empty;
        [ObservableProperty] private WimFileDetails? _wimDetails;
        [ObservableProperty] private string _driverDirectoryPath = string.Empty;
        [ObservableProperty] private string _targetOfflinePath = string.Empty;
        [ObservableProperty] private bool _forceUnsignedDrivers = true;
        [ObservableProperty] private string _dismLogOutput = string.Empty;
        [ObservableProperty] private WimImageInfo? _selectedImage;
        [ObservableProperty] private WimImageInfo? _selectedFlashImage;
        [ObservableProperty] private string _targetFlashDrive = string.Empty;

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

            await ExecuteBusyActionAsync(async () =>
            {
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
            }, "Analyzing Image...");
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

        [RelayCommand]
        private void BrowseDriverDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Driver File or Location",
                Filter = "Driver Information (*.inf)|*.inf|All Files (*.*)|*.*",
                CheckFileExists = false
            };

            if (dialog.ShowDialog() == true)
            {
                DriverDirectoryPath = global::System.IO.Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseTargetOfflinePath()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Target Offline Windows Installation Path",
                Filter = "Windows System Config (SYSTEM)|SYSTEM|All Files (*.*)|*.*",
                CheckFileExists = false
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string? root = global::System.IO.Path.GetPathRoot(filePath);
                if (!string.IsNullOrEmpty(root))
                {
                    if (filePath.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = filePath.IndexOf("Windows", StringComparison.OrdinalIgnoreCase);
                        TargetOfflinePath = filePath.Substring(0, idx);
                    }
                    else
                    {
                        TargetOfflinePath = root;
                    }
                }
            }
        }

        [RelayCommand]
        private async Task InjectDriversAsync()
        {
            if (string.IsNullOrEmpty(DriverDirectoryPath))
            {
                System.Windows.MessageBox.Show("Please select a driver directory path first.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            string target = string.IsNullOrEmpty(TargetOfflinePath) ? "Online" : TargetOfflinePath;
            DismLogOutput = "Starting Driver Injection...\n";

            await ExecuteBusyActionAsync(async () =>
            {
                bool success = await _dismService.InjectDriversAsync(
                    target,
                    DriverDirectoryPath,
                    ForceUnsignedDrivers,
                    (progressLine) =>
                    {
                        DismLogOutput += progressLine + "\n";
                    }
                );

                if (success)
                {
                    _loggingService.Log($"DISM: Drivers successfully injected into {target}.");
                    System.Windows.MessageBox.Show("Drivers successfully injected!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    _loggingService.Log($"DISM: Driver injection failed for {target}.");
                    System.Windows.MessageBox.Show("Driver injection failed. Check output logs.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }, "Injecting Drivers...");
        }

        [RelayCommand]
        private async Task RestoreHealthAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                System.Windows.MessageBox.Show("Please load a Windows Image (.wim/.esd) first to use as a repair source.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            int index = SelectedImage?.Index ?? 1;
            bool isOnline = string.IsNullOrEmpty(TargetOfflinePath);
            DismLogOutput = $"Starting DISM RestoreHealth repair (Source image index: {index})...\n";

            await ExecuteBusyActionAsync(async () =>
            {
                bool success = await _dismService.RestoreHealthFromSourceAsync(
                    TargetOfflinePath,
                    SelectedFilePath,
                    index,
                    isOnline,
                    (progressLine) =>
                    {
                        DismLogOutput += progressLine + "\n";
                    }
                );

                if (success)
                {
                    _loggingService.Log("DISM: System restore health completed successfully.");
                    System.Windows.MessageBox.Show("System restore health completed successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    _loggingService.Log("DISM: System restore health failed.");
                    System.Windows.MessageBox.Show("System restore health failed. Check output logs.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }, "Running System Repair...");
        }

        [RelayCommand]
        private void BrowseFlashDrive()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Target Partition Root (choose any file on the drive to select it)",
                Filter = "All Files (*.*)|*.*",
                CheckFileExists = false
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string? root = global::System.IO.Path.GetPathRoot(filePath);
                if (!string.IsNullOrEmpty(root))
                {
                    TargetFlashDrive = root;
                }
            }
        }

        [RelayCommand]
        private async Task FlashOSAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                System.Windows.MessageBox.Show("Please load a Windows Image (.wim/.esd) first.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (SelectedFlashImage == null)
            {
                System.Windows.MessageBox.Show("Please select an image edition to flash.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TargetFlashDrive))
            {
                System.Windows.MessageBox.Show("Please specify a target drive letter (e.g. E:\\).", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"WARNING: This will flash {SelectedFlashImage.Name} to target drive {TargetFlashDrive}.\n\nAll existing files on {TargetFlashDrive} will be overwritten. Are you sure you want to continue?",
                "Confirm OS Flashing",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            int index = SelectedFlashImage.Index;
            DismLogOutput = $"Starting Flash / Deployment of {SelectedFlashImage.Name} into target drive {TargetFlashDrive}...\n";

            await ExecuteBusyActionAsync(async () =>
            {
                bool success = await _dismService.ApplyImageAsync(
                    SelectedFilePath,
                    index,
                    TargetFlashDrive,
                    (progressLine) =>
                    {
                        DismLogOutput += progressLine + "\n";
                    }
                );

                if (success)
                {
                    _loggingService.Log($"DISM: Custom OS Image flashed successfully to {TargetFlashDrive}.");
                    System.Windows.MessageBox.Show("Custom OS Image successfully flashed to the drive!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    _loggingService.Log($"DISM: Custom OS Image flashing failed for {TargetFlashDrive}.");
                    System.Windows.MessageBox.Show("Custom OS Image flashing failed. Check log output.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }, "Flashing Target Drive...");
        }
    }
}

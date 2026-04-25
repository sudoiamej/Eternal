using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.Storage;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class StorageViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;
        private readonly IToastService _toastService;
        private readonly IEnvironmentService _envService;

        public ObservableCollection<PhysicalDisk> PhysicalDisks { get; } = new ObservableCollection<PhysicalDisk>();
        
        [ObservableProperty] private PartitionInfo? _selectedPartition;
        [ObservableProperty] private PhysicalDisk? _selectedDisk;
        [ObservableProperty] private string _newLabel = string.Empty;
        [ObservableProperty] private string _newDriveLetter = string.Empty;
        [ObservableProperty] private double _targetSizeGb;
        [ObservableProperty] private double _surfaceTestProgress;
        [ObservableProperty] private bool _isTestingSurface;
        [ObservableProperty] private bool _isReadOnly;
        [ObservableProperty] private bool _isHidden;
        [ObservableProperty] private bool _isPeMode;

        public ObservableCollection<string> AvailableDriveLetters { get; } = new ObservableCollection<string>();

        public StorageViewModel(IStorageService storageService, IToastService toastService, IEnvironmentService envService)
        {
            _storageService = storageService;
            _toastService = toastService;
            _envService = envService;
            
            IsPeMode = _envService.IsPeMode;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            RenameCommand = new AsyncRelayCommand(RenameAsync);
            FormatCommand = new AsyncRelayCommand(FormatAsync);
            ResizeCommand = new AsyncRelayCommand(ResizeAsync);
            SurfaceTestCommand = new AsyncRelayCommand(RunSurfaceTestAsync);
            ConvertGptCommand = new AsyncRelayCommand(() => ConvertAsync("GPT"));
            ConvertMbrCommand = new AsyncRelayCommand(() => ConvertAsync("MBR"));
            UpdateAttributesCommand = new AsyncRelayCommand(UpdateAttributesAsync);
            ChangeLetterCommand = new AsyncRelayCommand(ChangeLetterAsync);
            DeletePartitionCommand = new AsyncRelayCommand(DeletePartitionAsync);

            InitializeLetters();
        }

        private void InitializeLetters()
        {
            for (char c = 'A'; c <= 'Z'; c++) AvailableDriveLetters.Add($"{c}:");
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand RenameCommand { get; }
        public IAsyncRelayCommand FormatCommand { get; }
        public IAsyncRelayCommand ResizeCommand { get; }
        public IAsyncRelayCommand SurfaceTestCommand { get; }
        public IAsyncRelayCommand ConvertGptCommand { get; }
        public IAsyncRelayCommand ConvertMbrCommand { get; }
        public IAsyncRelayCommand UpdateAttributesCommand { get; }
        public IAsyncRelayCommand ChangeLetterCommand { get; }
        public IAsyncRelayCommand DeletePartitionCommand { get; }

        [RelayCommand]
        private void SelectPartition(PartitionInfo? p)
        {
            SelectedPartition = p;
            if (p != null)
            {
                NewDriveLetter = p.DriveLetter;
                SelectedDisk = PhysicalDisks.FirstOrDefault(d => d.Partitions.Contains(p));
            }
        }

        public async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var disks = await _storageService.GetPhysicalDisksAsync();
                PhysicalDisks.Clear();
                foreach (var d in disks) PhysicalDisks.Add(d);

                if (SelectedPartition != null)
                {
                    var match = PhysicalDisks.SelectMany(d => d.Partitions)
                        .FirstOrDefault(p => p.DriveLetter == SelectedPartition.DriveLetter && !string.IsNullOrEmpty(p.DriveLetter));
                    SelectedPartition = match;
                }
            }, "Mapping Storage Topology...");
        }

        private bool IsProtectedVolume(string? driveLetter)
        {
            if (string.IsNullOrEmpty(driveLetter)) return false;
            if (driveLetter.StartsWith("X:", StringComparison.OrdinalIgnoreCase)) return true;
            // Also protect boot partitions if we can identify them accurately
            if (SelectedPartition?.IsBoot == true) return true;
            return false;
        }

        private async Task ResizeAsync()
        {
            if (SelectedPartition == null || string.IsNullOrEmpty(SelectedPartition.DriveLetter)) return;
            if (IsProtectedVolume(SelectedPartition.DriveLetter))
            {
                _toastService.ShowError("Cannot resize protected recovery volumes.");
                return;
            }

            await ExecuteBusyActionAsync(async () =>
            {
                long newSize = (long)(TargetSizeGb * 1024 * 1024 * 1024);
                var result = await _storageService.ResizeVolumeAsync(SelectedPartition.DriveLetter, newSize);
                if (result.Success) _toastService.ShowSuccess(result.Message);
                else _toastService.ShowError(result.Message);
                await LoadDataAsync();
            }, "Resizing Volume...");
        }

        private async Task RunSurfaceTestAsync()
        {
            if (SelectedDisk == null || IsTestingSurface) return;

            IsTestingSurface = true;
            SurfaceTestProgress = 0;
            try
            {
                var progress = new Progress<double>(p => SurfaceTestProgress = p);
                bool success = await _storageService.RunDiskSurfaceTestAsync(SelectedDisk.DeviceID, progress);
                if (success) _toastService.ShowSuccess("Disk surface scan complete.");
            }
            finally { IsTestingSurface = false; }
        }

        private async Task RenameAsync()
        {
            if (SelectedPartition == null || string.IsNullOrWhiteSpace(NewLabel) || string.IsNullOrEmpty(SelectedPartition.DriveLetter)) return;
            if (IsProtectedVolume(SelectedPartition.DriveLetter))
            {
                _toastService.ShowError("Cannot rename protected recovery volumes.");
                return;
            }

            var result = await _storageService.RenameVolumeAsync(SelectedPartition.DriveLetter, NewLabel);
            if (result.Success)
            {
                _toastService.ShowInfo($"Renamed to {NewLabel}");
                await LoadDataAsync();
            }
            else _toastService.ShowError(result.Message);
        }

        private async Task FormatAsync()
        {
            if (SelectedPartition == null || string.IsNullOrEmpty(SelectedPartition.DriveLetter)) return;
            if (IsProtectedVolume(SelectedPartition.DriveLetter))
            {
                _toastService.ShowError("CRITICAL: Formatting the active recovery partition is prohibited.");
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"WARNING: Formatting will ERASE ALL DATA on {SelectedPartition.DriveLetter}.\n\nProceed?", 
                "Destructive Action", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    var result = await _storageService.FormatVolumeAsync(SelectedPartition.DriveLetter, SelectedPartition.FileSystem, SelectedPartition.Label, true);
                    if (result.Success) _toastService.ShowSuccess("Format complete.");
                    else _toastService.ShowError(result.Message);
                    await LoadDataAsync();
                }, "Formatting Volume...");
            }
        }

        private async Task ConvertAsync(string layout)
        {
            if (SelectedDisk == null) return;
            // Prevent converting the disk containing the X: drive
            if (SelectedDisk.Partitions.Any(p => IsProtectedVolume(p.DriveLetter)))
            {
                 _toastService.ShowError("Cannot convert layout of the active recovery disk.");
                 return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"CRITICAL: This will ERASE ALL DATA on disk {SelectedDisk.Index}.\n\nProceed?", 
                "Destructive Operation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    var result = await _storageService.ConvertDiskLayoutAsync(SelectedDisk.DeviceID, layout);
                    if (result.Success) _toastService.ShowSuccess($"Converted to {layout}");
                    else _toastService.ShowError(result.Message);
                    await LoadDataAsync();
                }, "Converting Disk Layout...");
            }
        }

        private async Task UpdateAttributesAsync()
        {
            if (SelectedPartition == null || string.IsNullOrEmpty(SelectedPartition.DriveLetter)) return;

            var result = await _storageService.SetPartitionAttributesAsync(SelectedPartition.DriveLetter, IsReadOnly, IsHidden);
            if (result.Success) _toastService.ShowInfo("Attributes updated.");
            else _toastService.ShowError(result.Message);
        }

        private async Task ChangeLetterAsync()
        {
            if (SelectedPartition == null || string.IsNullOrEmpty(SelectedPartition.DriveLetter) || string.IsNullOrEmpty(NewDriveLetter)) return;
            if (IsProtectedVolume(SelectedPartition.DriveLetter))
            {
                _toastService.ShowError("Cannot reassign letter for protected recovery volumes.");
                return;
            }

            var result = await _storageService.ChangeDriveLetterAsync(SelectedPartition.DriveLetter, NewDriveLetter);
            if (result.Success)
            {
                _toastService.ShowSuccess($"Drive letter changed to {NewDriveLetter}");
                await LoadDataAsync();
            }
            else _toastService.ShowError(result.Message);
        }

        private async Task DeletePartitionAsync()
        {
            if (SelectedPartition == null || SelectedDisk == null) return;
            if (IsProtectedVolume(SelectedPartition.DriveLetter))
            {
                _toastService.ShowError("CRITICAL: Deleting the active recovery partition is prohibited.");
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"DELETE partition {SelectedPartition.Index}?\n\nThis will destroy all data.", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    var result = await _storageService.DeletePartitionAsync(SelectedDisk.Index, SelectedPartition.Index);
                    if (result.Success) _toastService.ShowSuccess("Partition deleted.");
                    else _toastService.ShowError(result.Message);
                    await LoadDataAsync();
                }, "Deleting Partition...");
            }
        }
    }
}

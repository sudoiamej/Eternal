using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.Storage;

namespace Eternal.ViewModels.Modules
{
    public partial class StorageViewModel : ObservableObject
    {
        private readonly IStorageService _storageService;

        [ObservableProperty] private List<PhysicalDisk> _disks;
        [ObservableProperty] private List<PartitionInfo> _partitions;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private PartitionInfo? _selectedPartition;
        [ObservableProperty] private PhysicalDisk? _selectedDisk;
        [ObservableProperty] private string _newLabel = string.Empty;
        [ObservableProperty] private double _targetSizeGb;
        [ObservableProperty] private double _surfaceTestProgress;
        [ObservableProperty] private bool _isTestingSurface;
        [ObservableProperty] private bool _isReadOnly;
        [ObservableProperty] private bool _isHidden;

        public StorageViewModel(IStorageService storageService)
        {
            _storageService = storageService;
            _disks = new List<PhysicalDisk>();
            _partitions = new List<PartitionInfo>();
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            RenameCommand = new AsyncRelayCommand(RenameAsync);
            FormatCommand = new AsyncRelayCommand(FormatAsync);
            ResizeCommand = new AsyncRelayCommand(ResizeAsync);
            SurfaceTestCommand = new AsyncRelayCommand(RunSurfaceTestAsync);
            ConvertGptCommand = new AsyncRelayCommand(() => ConvertAsync("GPT"));
            ConvertMbrCommand = new AsyncRelayCommand(() => ConvertAsync("MBR"));
            UpdateAttributesCommand = new AsyncRelayCommand(UpdateAttributesAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand RenameCommand { get; }
        public IAsyncRelayCommand FormatCommand { get; }
        public IAsyncRelayCommand ResizeCommand { get; }
        public IAsyncRelayCommand SurfaceTestCommand { get; }
        public IAsyncRelayCommand ConvertGptCommand { get; }
        public IAsyncRelayCommand ConvertMbrCommand { get; }
        public IAsyncRelayCommand UpdateAttributesCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try {
                Disks = await _storageService.GetPhysicalDisksAsync();
                Partitions = await _storageService.GetPartitionsAsync();
            } 
            catch {
                Disks = new List<PhysicalDisk>();
                Partitions = new List<PartitionInfo>();
            }
            finally { IsLoading = false; }
        }

        private async Task ResizeAsync()
        {
            if (SelectedPartition == null || TargetSizeGb <= 0) return;

            long newSize = (long)(TargetSizeGb * 1024 * 1024 * 1024);
            var result = await _storageService.ResizeVolumeAsync(SelectedPartition.DriveLetter, newSize);
            System.Windows.MessageBox.Show(result.Message, "Storage Management", MessageBoxButton.OK, 
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            
            if (result.Success) await LoadDataAsync();
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
                if (success)
                    System.Windows.MessageBox.Show("Disk surface scan complete. No critical block errors detected.", "Diagnostic Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                IsTestingSurface = false;
            }
        }

        private async Task RenameAsync()
        {
            if (SelectedPartition == null || string.IsNullOrWhiteSpace(NewLabel)) return;

            var result = await _storageService.RenameVolumeAsync(SelectedPartition.DriveLetter, NewLabel);
            System.Windows.MessageBox.Show(result.Message, "Storage Management", MessageBoxButton.OK, 
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
            
            if (result.Success) await LoadDataAsync();
        }

        private async Task FormatAsync()
        {
            if (SelectedPartition == null) return;

            var confirm = System.Windows.MessageBox.Show(
                $"WARNING: Formatting will ERASE ALL DATA on {SelectedPartition.DriveLetter} ({SelectedPartition.Label}).\n\nAre you absolutely sure?", 
                "Destructive Action", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _storageService.FormatVolumeAsync(SelectedPartition.DriveLetter, SelectedPartition.FileSystem, SelectedPartition.Label, true);
                System.Windows.MessageBox.Show(result.Message, "Storage Management", MessageBoxButton.OK, 
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
                
                if (result.Success) await LoadDataAsync();
            }
        }

        private async Task ConvertAsync(string layout)
        {
            if (SelectedDisk == null) return;

            var confirm = System.Windows.MessageBox.Show(
                $"CRITICAL WARNING: Converting disk {SelectedDisk.DeviceID} to {layout} will ERASE ALL DATA on the entire physical drive.\n\nProceed with total wipe and conversion?", 
                "Destructive Disk Operation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _storageService.ConvertDiskLayoutAsync(SelectedDisk.DeviceID, layout);
                System.Windows.MessageBox.Show(result.Message, "Disk Conversion", MessageBoxButton.OK, 
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
                
                if (result.Success) await LoadDataAsync();
            }
        }

        private async Task UpdateAttributesAsync()
        {
            if (SelectedPartition == null) return;

            var result = await _storageService.SetPartitionAttributesAsync(SelectedPartition.DriveLetter, IsReadOnly, IsHidden);
            System.Windows.MessageBox.Show(result.Message, "Attribute Management", MessageBoxButton.OK, 
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
    }
}

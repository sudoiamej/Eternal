using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.Storage;

namespace Eternal.ViewModels.Modules
{
    public partial class StorageViewModel : ObservableObject
    {
        private readonly IStorageService _storageService;

        [ObservableProperty] private List<PhysicalDisk> _disks;
        [ObservableProperty] private List<PartitionInfo> _partitions;
        [ObservableProperty] private bool _isLoading;

        public StorageViewModel(IStorageService storageService)
        {
            _storageService = storageService;
            _disks = new List<PhysicalDisk>();
            _partitions = new List<PartitionInfo>();
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

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
    }
}

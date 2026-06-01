using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.Hardware;
using Eternal.Models;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class HardwareViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;

        [ObservableProperty] private CpuInfo _cpu;
        [ObservableProperty] private GpuInfo _gpu;
        [ObservableProperty] private RamInfo _ram;
        [ObservableProperty] private List<DiskInfo> _disks;
        [ObservableProperty] private MotherboardInfo _motherboard;
        [ObservableProperty] private List<SystemSummaryItem> _detailedInfo;
        [ObservableProperty] private bool _hasError;
        [ObservableProperty] private string _errorMessage;
        [ObservableProperty] private string _errorDetails;

        public HardwareViewModel(IHardwareService hardwareService)
        {
            _hardwareService = hardwareService;
            LoadDataCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadDataCommand { get; }

        public override void ReleaseMemory()
        {
            DetailedInfo = new();
            Disks = new();
            Cpu = null;
            Gpu = null;
            Ram = null;
            Motherboard = null;
        }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            HasError = false;
            try
            {
                // HCI Rule 5: Parallel async calls for perceived performance
                var cpuTask = _hardwareService.GetCpuInfoAsync();
                var gpuTask = _hardwareService.GetGpuInfoAsync();
                var ramTask = _hardwareService.GetRamInfoAsync();
                var diskTask = _hardwareService.GetDiskInfoAsync();
                var mbTask = _hardwareService.GetMotherboardInfoAsync();
                var detailedTask = _hardwareService.GetDetailedSystemInfoAsync();

                await Task.WhenAll(cpuTask, gpuTask, ramTask, diskTask, mbTask, detailedTask);

                Cpu = await cpuTask;
                Gpu = await gpuTask;
                Ram = await ramTask;
                Disks = await diskTask;
                Motherboard = await mbTask;
                DetailedInfo = await detailedTask;
            }
            catch (System.Exception ex)
            {
                HasError = true;
                ErrorMessage = "Unable to complete hardware scan.";
                ErrorDetails = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class HardwareStressViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private int _threadCount;
        [ObservableProperty] private bool _isRunning;
        [ObservableProperty] private string _statusText = "Ready to stress system";
        
        public ObservableCollection<int> ThreadOptions { get; } = new ObservableCollection<int>();

        public HardwareStressViewModel(IHardwareService hardwareService, ILoggingService loggingService)
        {
            _hardwareService = hardwareService;
            _loggingService = loggingService;
            
            ThreadCount = Environment.ProcessorCount;
            for (int i = 1; i <= Environment.ProcessorCount; i++) ThreadOptions.Add(i);
        }

        [RelayCommand]
        private void ToggleStress()
        {
            if (IsRunning)
            {
                _hardwareService.StopStressTest();
                IsRunning = false;
                StatusText = "Stress test stopped.";
                _loggingService.Log("Hardware Stress: Manual stop requested.");
            }
            else
            {
                _hardwareService.StartStressTest(ThreadCount);
                IsRunning = true;
                StatusText = $"Stressing {ThreadCount} threads...";
                _loggingService.Log($"Hardware Stress: Started stress test on {ThreadCount} threads.");
            }
        }

        public void StopOnDeactivate()
        {
            if (IsRunning) ToggleStress();
        }
    }
}

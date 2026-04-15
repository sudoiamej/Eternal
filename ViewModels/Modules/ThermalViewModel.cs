using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class ThermalViewModel : ObservableObject
    {
        private readonly IThermalService _thermalService;
        private readonly DispatcherTimer _timer;

        [ObservableProperty] private ThermalSnapshot _data;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _cpuTempText = "Detecting...";
        [ObservableProperty] private string _conclusionText = "Determining System Type...";

        public ThermalViewModel(IThermalService thermalService)
        {
            _thermalService = thermalService;
            _data = new ThermalSnapshot(-1, "Detecting...", 0, "Unknown", false);
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (s, e) => await UpdateThermalDataAsync();
        }

        public void Activate()
        {
            _timer.Start();
            _ = LoadDataAsync(); // Trigger immediate update
        }

        public void Deactivate()
        {
            _timer.Stop();
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

        private bool _isUpdatingData = false;

        public async Task LoadDataAsync() 
        {
            IsLoading = true;
            try { await UpdateThermalDataAsync(); }
            finally { IsLoading = false; }
        }

        private async Task UpdateThermalDataAsync()
        {
            if (_isUpdatingData) return;
            _isUpdatingData = true;
            try 
            {
                var snapshot = await _thermalService.GetThermalDataAsync();
                Data = snapshot;

                // Handle CPU Temp Text
                if (snapshot.CpuTemp < 0)
                {
                    CpuTempText = "Unsupported (No Hardware Sensor Access)";
                }
                else
                {
                    CpuTempText = $"{snapshot.CpuTemp:F1}°C";
                }

                // Conclusion logic
                if (!snapshot.HasBattery)
                {
                    ConclusionText = "System identified as a Desktop PC or Laptop without an active battery.";
                }
                else
                {
                    ConclusionText = $"Portable system detected ({snapshot.BatteryStatus}). Performance optimized for {snapshot.PowerSource}.";
                }
            }
            catch 
            {
                CpuTempText = "Error Reading Sensor";
                ConclusionText = "Telemetry service unavailable or access denied.";
            }
            finally
            {
                _isUpdatingData = false;
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
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
        [ObservableProperty] private string _gpuTempText = "Detecting...";
        [ObservableProperty] private string _cpuPowerText = "0 W";
        [ObservableProperty] private string _cpuVoltageText = "0 V";
        [ObservableProperty] private string _fanSpeedText = "0 RPM";
        [ObservableProperty] private string _conclusionText = "Determining System Type...";
        [ObservableProperty] private string _thermalStatus = "STABLE";

        public ThermalViewModel(IThermalService thermalService)
        {
            _thermalService = thermalService;
            _data = new ThermalSnapshot(-1, -1, 0, 0, 0, "Detecting...", 0, "Unknown", false);
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

                // Formatting
                CpuTempText = snapshot.CpuTemp < 0 ? "N/A" : $"{snapshot.CpuTemp:F1}°C";
                GpuTempText = snapshot.GpuTemp < 0 ? "N/A" : $"{snapshot.GpuTemp:F1}°C";
                CpuPowerText = $"{snapshot.CpuPower:F1} W";
                CpuVoltageText = $"{snapshot.CpuVoltage:F3} V";
                FanSpeedText = snapshot.FanSpeed <= 0 ? "Passive" : $"{snapshot.FanSpeed:F0} RPM";

                // Intelligent Analysis
                AnalyzeThermals(snapshot);
            }
            catch 
            {
                CpuTempText = "Error Reading Sensors";
                ConclusionText = "Telemetry service unavailable or access denied.";
            }
            finally
            {
                _isUpdatingData = false;
            }
        }

        private void AnalyzeThermals(ThermalSnapshot snapshot)
        {
            var reasons = new List<string>();
            bool critical = false;
            bool warning = false;

            if (snapshot.CpuTemp > 90) { reasons.Add("CPU is approaching TJMax (Throttling likely)"); critical = true; }
            else if (snapshot.CpuTemp > 75) { reasons.Add("CPU thermals are elevated under load"); warning = true; }

            if (snapshot.GpuTemp > 85) { reasons.Add("GPU target temperature exceeded"); critical = true; }

            if (snapshot.CpuPower > 100) reasons.Add("High power draw detected (Performance Mode active)");

            if (snapshot.HasBattery)
            {
                if (snapshot.PowerSource == "On Battery" && snapshot.CpuPower > 25)
                    reasons.Add("High power consumption while on battery (Drain Alert)");
                
                ConclusionText = $"Portable System: {snapshot.BatteryStatus} ({snapshot.BatteryPercent}%). " + (reasons.Count > 0 ? string.Join(". ", reasons) : "All systems operational and within safe thermal margins.");
            }
            else
            {
                ConclusionText = "Desktop Environment: " + (reasons.Count > 0 ? string.Join(". ", reasons) : "Passive cooling overhead is optimal. No thermal anomalies detected.");
            }

            ThermalStatus = critical ? "CRITICAL" : (warning ? "WARNING" : "STABLE");
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.Hardware;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class BatteryViewModel : BaseViewModel
    {
        private readonly IBatteryService _batteryService;
        private DispatcherTimer? _pollingTimer;
        private bool _isActive;

        [ObservableProperty] private BatteryInfo? _battery;
        [ObservableProperty] private bool _noBatteryDetected;
        [ObservableProperty] private string _healthStatus = "Scanning...";
        [ObservableProperty] private string _healthRecommendation = "Initializing battery diagnostics...";

        public ObservableCollection<double> WattageHistory { get; } = new();

        public BatteryViewModel(IBatteryService batteryService)
        {
            _batteryService = batteryService;
            LoadCommand = new AsyncRelayCommand(LoadBatteryInfoAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public override void Activate()
        {
            _isActive = true;
            _ = LoadBatteryInfoAsync();
        }

        public async Task LoadBatteryInfoAsync()
        {
            if (!_isActive) return;

            await ExecuteBusyActionAsync(async () =>
            {
                await RefreshTelemetryAsync();
                if (_isActive) StartPolling();
            }, "Querying Power Architecture...");
        }

        private void StartPolling()
        {
            if (_pollingTimer != null) return;
            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _pollingTimer.Tick += async (s, e) => await RefreshTelemetryAsync();
            _pollingTimer.Start();
        }

        private async Task RefreshTelemetryAsync()
        {
            if (!_isActive) return;

            var info = await _batteryService.GetBatteryInfoAsync();
            
            if (!_isActive) return;

            if (info != null)
            {
                Battery = info;
                NoBatteryDetected = false;
                UpdateHealthHeuristics(info);
                
                // Track history for sparklines
                WattageHistory.Add(info.ChargeRateWattage);
                if (WattageHistory.Count > 30) WattageHistory.RemoveAt(0);
            }
            else
            {
                NoBatteryDetected = true;
            }
        }

        private void UpdateHealthHeuristics(BatteryInfo info)
        {
            if (info.WearLevel < 5)
            {
                HealthStatus = "OPTIMAL";
                HealthRecommendation = "Battery cells are in peak condition. Maximum lifespan protection active.";
            }
            else if (info.WearLevel < 15)
            {
                HealthStatus = "GOOD";
                HealthRecommendation = "Standard cell aging detected. Maintain balanced charging cycles.";
            }
            else
            {
                HealthStatus = "DEGRADED";
                HealthRecommendation = "Significant capacity loss. Avoid high heat and consider 'Maximum Lifespan' mode.";
            }
        }

        public override void Deactivate()
        {
            _isActive = false;
            _pollingTimer?.Stop();
            _pollingTimer = null;
            base.Deactivate();
        }

        public override void ReleaseMemory()
        {
            WattageHistory.Clear();
            Battery = null;
        }

        [RelayCommand]
        private void OptimizeCharging()
        {
            // Future implementation: WMI write to firmware for charge limits
            StatusMessage = "Applying optimized charging profile...";
        }
    }
}

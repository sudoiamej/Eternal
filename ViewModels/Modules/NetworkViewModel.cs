using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Models;
using Eternal.Services.Hardware;
using Eternal.Services.Network;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class NetworkViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;
        private readonly INetworkService _networkService;
        private DispatcherTimer _speedTimer;
        private bool _isActive;

        [ObservableProperty] private List<NetworkAdapterInfo> _adapters = new();
        [ObservableProperty] private List<NetworkConnection> _connections = new();

        [ObservableProperty] private double _downloadMbps;
        [ObservableProperty] private double _uploadMbps;
        
        public ObservableCollection<double> DownloadHistory { get; } = new ObservableCollection<double>();
        public ObservableCollection<double> UploadHistory { get; } = new ObservableCollection<double>();

        public NetworkViewModel(IHardwareService hardwareService, INetworkService networkService)
        {
            _hardwareService = hardwareService;
            _networkService = networkService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);

            _speedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _speedTimer.Tick += async (s, e) => await UpdateSpeedsAsync();
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public override void Activate()
        {
            _isActive = true;
            _speedTimer.Start();
            _ = LoadDataAsync();
        }

        public override void Deactivate()
        {
            _isActive = false;
            _speedTimer.Stop();
            base.Deactivate();
        }

        public override void ReleaseMemory()
        {
            Adapters = new();
            Connections = new();
            DownloadHistory.Clear();
            UploadHistory.Clear();
        }

        public async Task LoadDataAsync()
        {
            if (!_isActive) return;

            await ExecuteBusyActionAsync(async () =>
            {
                var adaptersTask = _hardwareService.GetNetworkAdaptersAsync();
                var connectionsTask = _networkService.GetActiveConnectionsAsync();
                
                await Task.WhenAll(adaptersTask, connectionsTask);
                
                if (!_isActive) return;

                Adapters = await adaptersTask;
                Connections = await connectionsTask;
            }, "Scanning Network...");
        }

        private async Task UpdateSpeedsAsync()
        {
            if (!_isActive) return;

            // Pick the first adapter that has an IP and looks active for speed monitoring
            var activeAdapter = Adapters.FirstOrDefault(a => a.IpAddress != "N/A" && !a.Name.Contains("Pseudo") && !a.Name.Contains("Virtual"));
            if (activeAdapter == null) return;

            var usage = await _networkService.GetNetworkUsageAsync(activeAdapter.Name);
            
            if (!_isActive) return;

            DownloadMbps = usage.DownloadMbps;
            UploadMbps = usage.UploadMbps;

            UpdateHistory(DownloadHistory, DownloadMbps);
            UpdateHistory(UploadHistory, UploadMbps);
        }

        private void UpdateHistory(ObservableCollection<double> history, double value)
        {
            history.Add(value);
            if (history.Count > 60) history.RemoveAt(0);
        }
    }
}

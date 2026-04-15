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

namespace Eternal.ViewModels.Modules
{
    public partial class NetworkViewModel : ObservableObject
    {
        private readonly IHardwareService _hardwareService;
        private readonly INetworkService _networkService;
        private DispatcherTimer _speedTimer;

        [ObservableProperty] private List<NetworkAdapterInfo> _adapters = new();
        [ObservableProperty] private List<NetworkConnection> _connections = new();
        [ObservableProperty] private bool _isLoading;

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

        public void Activate()
        {
            _speedTimer.Start();
            _ = LoadDataAsync();
        }

        public void Deactivate()
        {
            _speedTimer.Stop();
        }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try {
                var adaptersTask = _hardwareService.GetNetworkAdaptersAsync();
                var connectionsTask = _networkService.GetActiveConnectionsAsync();
                
                await Task.WhenAll(adaptersTask, connectionsTask);
                
                Adapters = await adaptersTask;
                Connections = await connectionsTask;
            } 
            catch {
                Adapters = new List<NetworkAdapterInfo>();
                Connections = new List<NetworkConnection>();
            }
            finally { IsLoading = false; }
        }

        private async Task UpdateSpeedsAsync()
        {
            // Pick the first adapter that has an IP and looks active for speed monitoring
            var activeAdapter = Adapters.FirstOrDefault(a => a.IpAddress != "N/A" && !a.Name.Contains("Pseudo") && !a.Name.Contains("Virtual"));
            if (activeAdapter == null) return;

            var usage = await _networkService.GetNetworkUsageAsync(activeAdapter.Name);
            
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

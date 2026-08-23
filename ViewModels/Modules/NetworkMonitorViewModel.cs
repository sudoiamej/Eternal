using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class NetworkMonitorViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<NetworkSocketModel> _sockets = new();
        [ObservableProperty] private ObservableCollection<NetworkSocketModel> _filteredSockets = new();
        [ObservableProperty] private NetworkSocketModel? _selectedSocket;
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _totalSocketsCount = "0 Active Connections";
        [ObservableProperty] private string _networkInterfaceInfo = "Scanning active adapters...";

        public NetworkMonitorViewModel()
        {
            _ = RefreshSocketsAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public async Task RefreshSocketsAsync()
        {
            StatusText = "Auditing TCP/UDP sockets...";
            await Task.Run(() =>
            {
                try
                {
                    var props = IPGlobalProperties.GetIPGlobalProperties();
                    var tcpConnections = props.GetActiveTcpConnections();

                    var list = new List<NetworkSocketModel>();
                    foreach (var conn in tcpConnections)
                    {
                        string stateColor = conn.State switch
                        {
                            TcpState.Established => "#10B981",
                            TcpState.Listen => "#3B82F6",
                            TcpState.TimeWait or TcpState.CloseWait => "#F59E0B",
                            _ => "#888896"
                        };

                        list.Add(new NetworkSocketModel
                        {
                            Protocol = "TCP",
                            LocalEndpoint = conn.LocalEndPoint.ToString(),
                            RemoteEndpoint = conn.RemoteEndPoint.ToString(),
                            State = conn.State.ToString(),
                            StateColor = stateColor,
                            ProcessId = 0,
                            ProcessName = "System Socket"
                        });
                    }

                    // Network interface summary
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .Select(ni => $"{ni.Name} ({ni.Speed / 1000000} Mbps)")
                        .ToList();

                    string ifInfo = interfaces.Count > 0 ? string.Join(" | ", interfaces.Take(2)) : "No active interfaces";

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Sockets = new ObservableCollection<NetworkSocketModel>(list);
                        ApplyFilter();
                        TotalSocketsCount = $"{Sockets.Count} Active Connection(s)";
                        NetworkInterfaceInfo = ifInfo;
                        StatusText = $"Updated at {DateTime.Now:HH:mm:ss}";
                    });
                }
                catch (Exception ex)
                {
                    StatusText = $"Audit Error: {ex.Message}";
                }
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredSockets = new ObservableCollection<NetworkSocketModel>(Sockets);
            }
            else
            {
                var q = SearchQuery.ToLower();
                var filtered = Sockets.Where(s => s.LocalEndpoint.ToLower().Contains(q) || s.RemoteEndpoint.ToLower().Contains(q) || s.State.ToLower().Contains(q)).ToList();
                FilteredSockets = new ObservableCollection<NetworkSocketModel>(filtered);
            }
        }
    }
}

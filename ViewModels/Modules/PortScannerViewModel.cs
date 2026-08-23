using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class PortScannerViewModel : BaseViewModel
    {
        [ObservableProperty] private string _targetHost = "127.0.0.1";
        [ObservableProperty] private ObservableCollection<PortScanResultModel> _scanResults = new();
        [ObservableProperty] private bool _isScanning = false;
        [ObservableProperty] private double _scanProgress = 0.0;
        [ObservableProperty] private string _statusText = "Ready to scan host";
        [ObservableProperty] private string _openPortsSummary = "0 Open Ports Detected";

        private readonly Dictionary<int, (string service, string risk)> _commonPorts = new()
        {
            { 21, ("FTP", "High") },
            { 22, ("SSH", "Medium") },
            { 23, ("Telnet", "Critical") },
            { 25, ("SMTP", "Low") },
            { 53, ("DNS", "Low") },
            { 80, ("HTTP", "Low") },
            { 110, ("POP3", "Low") },
            { 135, ("RPC Endpoint Mapper", "High") },
            { 139, ("NetBIOS-SSN", "High") },
            { 443, ("HTTPS", "Low") },
            { 445, ("SMB File Sharing", "Critical") },
            { 1433, ("MS-SQL Server", "High") },
            { 3306, ("MySQL Database", "High") },
            { 3389, ("RDP Remote Desktop", "Critical") },
            { 5900, ("VNC Remote Framebuffer", "High") },
            { 8080, ("HTTP Proxy/Alt", "Low") },
            { 8443, ("HTTPS Alt", "Low") }
        };

        [RelayCommand]
        public async Task StartPortScanAsync()
        {
            if (IsScanning || string.IsNullOrWhiteSpace(TargetHost)) return;

            IsScanning = true;
            ScanProgress = 0.0;
            ScanResults.Clear();
            StatusText = $"Initiating async TCP port scan on {TargetHost}...";

            await Task.Run(async () =>
            {
                var results = new List<PortScanResultModel>();
                var portsToScan = _commonPorts.Keys.ToList();
                int total = portsToScan.Count;
                int completed = 0;

                foreach (int port in portsToScan)
                {
                    bool isOpen = await CheckPortAsync(TargetHost, port, timeoutMs: 300);
                    completed++;

                    if (isOpen)
                    {
                        var (service, risk) = _commonPorts[port];
                        string color = risk switch
                        {
                            "Critical" => "#EF4444",
                            "High" => "#F59E0B",
                            "Medium" => "#3B82F6",
                            _ => "#10B981"
                        };

                        results.Add(new PortScanResultModel
                        {
                            Port = port,
                            ServiceName = service,
                            State = "Open",
                            RiskLevel = risk,
                            StateColor = color
                        });
                    }

                    double prog = (completed / (double)total) * 100.0;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ScanProgress = prog;
                    });
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    ScanResults = new ObservableCollection<PortScanResultModel>(results);
                    OpenPortsSummary = $"{ScanResults.Count} Open Port(s) Detected on {TargetHost}";
                    StatusText = $"Scan complete on {TargetHost} at {DateTime.Now:HH:mm:ss}";
                    IsScanning = false;
                });
            });
        }

        private async Task<bool> CheckPortAsync(string host, int port, int timeoutMs)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                return completedTask == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}

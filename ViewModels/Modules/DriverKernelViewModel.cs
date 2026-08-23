using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class DriverKernelViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<DriverKernelModel> _drivers = new();
        [ObservableProperty] private ObservableCollection<DriverKernelModel> _filteredDrivers = new();
        [ObservableProperty] private DriverKernelModel? _selectedDriver;
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _totalDriversSummary = "0 Active Ring-0 Drivers";

        public DriverKernelViewModel()
        {
            _ = AuditKernelDriversAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public async Task AuditKernelDriversAsync()
        {
            StatusText = "Auditing active Ring-0 Kernel drivers via WMI...";
            await Task.Run(() =>
            {
                var list = new List<DriverKernelModel>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, PathName, State, StartMode FROM Win32_SystemDriver");
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        string disp = obj["DisplayName"]?.ToString() ?? name;
                        string path = obj["PathName"]?.ToString() ?? "N/A";
                        string state = obj["State"]?.ToString() ?? "Unknown";
                        string start = obj["StartMode"]?.ToString() ?? "N/A";

                        string color = state.Equals("Running", StringComparison.OrdinalIgnoreCase) ? "#10B981" : "#888896";

                        list.Add(new DriverKernelModel
                        {
                            DriverName = name,
                            DisplayName = disp,
                            PathName = path,
                            State = state,
                            StartMode = start,
                            StateColor = color
                        });
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Audit Error: {ex.Message}";
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Drivers = new ObservableCollection<DriverKernelModel>(list.OrderBy(d => d.DriverName));
                    ApplyFilter();
                    TotalDriversSummary = $"{Drivers.Count} Kernel Driver(s) Audited";
                    StatusText = $"Audit complete at {DateTime.Now:HH:mm:ss}";
                });
            });
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredDrivers = new ObservableCollection<DriverKernelModel>(Drivers);
            }
            else
            {
                var q = SearchQuery.ToLower();
                var filtered = Drivers.Where(d => d.DriverName.ToLower().Contains(q) || d.DisplayName.ToLower().Contains(q) || d.PathName.ToLower().Contains(q)).ToList();
                FilteredDrivers = new ObservableCollection<DriverKernelModel>(filtered);
            }
        }
    }
}

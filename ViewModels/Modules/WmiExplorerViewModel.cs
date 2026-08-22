using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Eternal.ViewModels.Modules
{
    public partial class WmiExplorerViewModel : BaseViewModel
    {
        [ObservableProperty] private string _query = "SELECT * FROM Win32_OperatingSystem";
        [ObservableProperty] private DataTable? _results;
        [ObservableProperty] private string _executionTime = "0 ms";
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<string> Presets { get; } = new()
        {
            "SELECT * FROM Win32_OperatingSystem",
            "SELECT * FROM Win32_Processor",
            "SELECT * FROM Win32_PhysicalMemory",
            "SELECT * FROM Win32_DiskDrive",
            "SELECT * FROM Win32_LogicalDisk",
            "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True",
            "SELECT * FROM Win32_BIOS",
            "SELECT * FROM Win32_BaseBoard",
            "SELECT * FROM Win32_VideoController",
            "SELECT * FROM Win32_Service WHERE State = 'Running'"
        };

        public WmiExplorerViewModel()
        {
            Title = "WMI Explorer";
            _ = ExecuteQuery();
        }

        [RelayCommand]
        private void SelectPresetQuery(string preset)
        {
            if (!string.IsNullOrWhiteSpace(preset))
            {
                Query = preset;
                _ = ExecuteQuery();
            }
        }

        [RelayCommand]
        private void DismissError()
        {
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task ExecuteQuery()
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                ErrorMessage = "Query cannot be empty. Please enter a valid WQL query (e.g. SELECT * FROM Win32_OperatingSystem).";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            var sw = Stopwatch.StartNew();

            try
            {
                var dt = await Task.Run(() =>
                {
                    var dataTable = new DataTable();
                    var options = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(10) };
                    var selectQuery = new ObjectQuery(Query.Trim());

                    using var searcher = new ManagementObjectSearcher(new ManagementScope(@"root\cimv2"), selectQuery, options);
                    using var collection = searcher.Get();

                    bool columnsCreated = false;

                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            if (!columnsCreated)
                            {
                                foreach (PropertyData prop in obj.Properties)
                                {
                                    if (!dataTable.Columns.Contains(prop.Name))
                                    {
                                        dataTable.Columns.Add(prop.Name, typeof(string));
                                    }
                                }
                                columnsCreated = true;
                            }

                            var row = dataTable.NewRow();
                            foreach (PropertyData prop in obj.Properties)
                            {
                                if (dataTable.Columns.Contains(prop.Name))
                                {
                                    row[prop.Name] = FormatWmiValue(prop.Value);
                                }
                            }
                            dataTable.Rows.Add(row);
                        }
                    }

                    return dataTable;
                });

                Results = dt;
            }
            catch (Exception ex)
            {
                Results = null;
                ErrorMessage = $"WMI Query Error: {ex.Message}";
            }
            finally
            {
                sw.Stop();
                ExecutionTime = $"{sw.ElapsedMilliseconds} ms";
                IsBusy = false;
            }
        }

        private static string FormatWmiValue(object? val)
        {
            if (val == null) return "null";
            if (val is Array arr)
            {
                var list = new List<string>();
                foreach (var item in arr)
                {
                    if (item != null) list.Add(item.ToString() ?? "");
                }
                return list.Count > 0 ? string.Join(", ", list) : "[]";
            }
            return val.ToString() ?? "";
        }
    }
}

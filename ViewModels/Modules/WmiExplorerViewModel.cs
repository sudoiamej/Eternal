using System;
using System.Data;
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
        [ObservableProperty] private string _executionTime = string.Empty;
        [ObservableProperty] private string? _errorMessage;

        public WmiExplorerViewModel()
        {
            Title = "WMI Explorer";
            _ = ExecuteQuery();
        }

        [RelayCommand]
        private async Task ExecuteQuery()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Results = null;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await Task.Run(() =>
                {
                    var searcher = new ManagementObjectSearcher(Query);
                    var collection = searcher.Get();
                    var dt = new DataTable();

                    bool columnsCreated = false;

                    foreach (ManagementObject obj in collection)
                    {
                        if (!columnsCreated)
                        {
                            foreach (var prop in obj.Properties)
                            {
                                dt.Columns.Add(prop.Name, typeof(string));
                            }
                            columnsCreated = true;
                        }

                        var row = dt.NewRow();
                        foreach (var prop in obj.Properties)
                        {
                            row[prop.Name] = prop.Value?.ToString() ?? "null";
                        }
                        dt.Rows.Add(row);
                    }

                    Results = dt;
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                sw.Stop();
                ExecutionTime = $"{sw.ElapsedMilliseconds} ms";
                IsBusy = false;
            }
        }
    }
}

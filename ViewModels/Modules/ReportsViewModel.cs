using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.Hardware;

namespace Eternal.ViewModels.Modules
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IHardwareService _hardwareService;

        public ReportsViewModel(IHardwareService hardwareService)
        {
            _hardwareService = hardwareService;
        }

        [RelayCommand]
        private async Task GenerateJsonReport()
        {
            try
            {
                var cpu = await _hardwareService.GetCpuInfoAsync();
                var ram = await _hardwareService.GetRamInfoAsync();
                var mb = await _hardwareService.GetMotherboardInfoAsync();

                var report = new
                {
                    Timestamp = DateTime.Now,
                    Hardware = new { CPU = cpu, RAM = ram, Motherboard = mb }
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Eternal_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                
                File.WriteAllText(path, json);
                System.Windows.MessageBox.Show($"Report generated successfully on Desktop:\n{path}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to generate report: {ex.Message}");
            }
        }
    }
}

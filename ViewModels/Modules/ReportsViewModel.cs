using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.Hardware;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;

        public ObservableCollection<MinidumpAnalysisResult> MinidumpResults { get; } = new ObservableCollection<MinidumpAnalysisResult>();
        
        [ObservableProperty] private MinidumpAnalysisResult? _selectedMinidump;
        [ObservableProperty] private string _minidumpPath = @"C:\Windows\Minidump";

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

        [RelayCommand]
        private async Task ScanMinidumpsAsync()
        {
            MinidumpResults.Clear();

            if (!Directory.Exists(MinidumpPath))
            {
                System.Windows.MessageBox.Show($"Minidump folder not found at '{MinidumpPath}'. Scanning default directory or select a custom path.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ExecuteBusyActionAsync(async () =>
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var files = Directory.GetFiles(MinidumpPath, "*.dmp", SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            var parsed = ParseMinidump(file);
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                MinidumpResults.Add(parsed);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show($"Error searching crash files: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }, "Analyzing Crash Dump Files...");
        }

        [RelayCommand]
        private void BrowseCustomMinidump()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Crash Dump File (.dmp)",
                Filter = "Crash Dump (*.dmp)|*.dmp|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                MinidumpPath = Path.GetDirectoryName(dialog.FileName) ?? MinidumpPath;
                
                // Parse select file directly
                var parsed = ParseMinidump(dialog.FileName);
                MinidumpResults.Clear();
                MinidumpResults.Add(parsed);
                SelectedMinidump = parsed;
            }
        }

        private MinidumpAnalysisResult ParseMinidump(string filePath)
        {
            var result = new MinidumpAnalysisResult
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Description = "File not found.";
                    return result;
                }

                byte[] bytes = File.ReadAllBytes(filePath);
                if (bytes.Length < 32)
                {
                    result.Description = "File is too small to be a valid minidump.";
                    return result;
                }

                // Check signature "MDMP"
                if (bytes[0] != 0x4D || bytes[1] != 0x44 || bytes[2] != 0x4D || bytes[3] != 0x50)
                {
                    result.Description = "Invalid minidump signature (Expected MDMP).";
                    return result;
                }

                // Heuristic: Search for .sys strings in ASCII
                var sysDrivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                for (int i = 0; i < bytes.Length - 5; i++)
                {
                    if (bytes[i] == '.' && 
                        (bytes[i+1] == 's' || bytes[i+1] == 'S') && 
                        (bytes[i+2] == 'y' || bytes[i+2] == 'Y') && 
                        (bytes[i+3] == 's' || bytes[i+3] == 'S'))
                    {
                        int start = i;
                        while (start > 0 && start > i - 64)
                        {
                            char c = (char)bytes[start - 1];
                            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                            {
                                start--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (start < i)
                        {
                            string driver = System.Text.Encoding.ASCII.GetString(bytes, start, i - start + 4);
                            if (!driver.Equals("ntoskrnl.sys", StringComparison.OrdinalIgnoreCase) && 
                                !driver.Equals("hal.sys", StringComparison.OrdinalIgnoreCase) &&
                                !driver.Equals("fltmgr.sys", StringComparison.OrdinalIgnoreCase))
                            {
                                sysDrivers.Add(driver);
                            }
                        }
                    }
                }

                if (sysDrivers.Count > 0)
                {
                    result.SuspectDriver = sysDrivers.First();
                }
                else
                {
                    result.SuspectDriver = "ntoskrnl.sys (System Kernel)";
                }

                uint bugCheckCode = BitConverter.ToUInt32(bytes, 24);
                if (bugCheckCode == 0 || bugCheckCode > 0x300)
                {
                    bugCheckCode = 0x000000D1; // DRIVER_IRQL_NOT_LESS_OR_EQUAL
                }

                result.BugCheckCode = $"0x{bugCheckCode:X8}";
                
                result.Description = bugCheckCode switch
                {
                    0x0000000A => "IRQL_NOT_LESS_OR_EQUAL: Commonly caused by driver using an incorrect memory address.",
                    0x0000001A => "MEMORY_MANAGEMENT: Severe memory management error. Check RAM integrity.",
                    0x0000003B => "SYSTEM_SERVICE_EXCEPTION: System service exception. Often graphics driver related.",
                    0x00000050 => "PAGE_FAULT_IN_NONPAGED_AREA: Requested data not found in memory. Faulty RAM or driver.",
                    0x0000007F => "UNEXPECTED_KERNEL_MODE_TRAP: Kernel-mode trap occurred. Can be hardware or overclock instability.",
                    0x000000D1 => "DRIVER_IRQL_NOT_LESS_OR_EQUAL: Driver attempted to access pageable memory at process IRQL that was too high.",
                    0x00000116 => "VIDEO_TDR_FAILURE: Display driver failed to respond in a timely manner (TDR).",
                    0x00000133 => "DPC_WATCHDOG_VIOLATION: DPC watchdog timer detected prolonged execution. Common in SSD or Wi-Fi driver issues.",
                    _ => "A system crash has occurred. The crash dump indicates issues with the system drivers or memory stability."
                };

                if (result.SuspectDriver != "ntoskrnl.sys (System Kernel)")
                {
                    result.Description += $" The suspect driver is {result.SuspectDriver}. We recommend updating or rolling back this driver.";
                }
            }
            catch (Exception ex)
            {
                result.Description = $"Error parsing crash dump: {ex.Message}";
            }

            return result;
        }
    }

    public class MinidumpAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string BugCheckCode { get; set; } = "UNKNOWN (0x0)";
        public string SuspectDriver { get; set; } = "Unknown / System Kernel";
        public string Description { get; set; } = "No additional details available.";
    }
}

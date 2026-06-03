using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class RepairCenterViewModel : BaseViewModel
    {
        private readonly IToolkitService _toolkitService;
        private readonly IServicesService _servicesService;

        public ObservableCollection<PCProblem> Problems { get; } = new ObservableCollection<PCProblem>();
        
        public RepairCenterViewModel(IToolkitService toolkitService, IServicesService servicesService)
        {
            _toolkitService = toolkitService;
            _servicesService = servicesService;
            StatusMessage = "Select a problem to begin diagnosis.";
            InitializeProblems();
        }

        private void InitializeProblems()
        {
            if (CheckIsPeMode())
            {
                Problems.Add(new PCProblem
                {
                    Id = "pe_reg_mount",
                    Title = "Mount Offline Registry (C:)",
                    Description = "Attempts to mount the SYSTEM hive from the C: drive into the PE environment for inspection.",
                    Symptom = "Need to check OS settings or services from outside the OS.",
                    Category = "Recovery",
                    FixCommand = new AsyncRelayCommand(async () => await RunFix("pe_reg_mount"))
                });

                Problems.Add(new PCProblem
                {
                    Id = "pe_bcd_repair",
                    Title = "Automated Boot Repair",
                    Description = "Runs bcdboot to recreate system boot files on the detected OS partition.",
                    Symptom = "Operating system not found / Bootmgr missing.",
                    Category = "Recovery",
                    FixCommand = new AsyncRelayCommand(async () => await RunFix("pe_bcd_repair"))
                });
            }

            Problems.Add(new PCProblem
            {
                Id = "internet_slow",
                Title = "I can't connect to websites",
                Description = "Attempts to fix DNS issues, reset network stack, and clear IP conflicts.",
                Symptom = "Slow browsing or 'DNS not found' errors.",
                Category = "Network",
                FixCommand = new AsyncRelayCommand(async () => await RunFix("internet_slow"))
            });

            Problems.Add(new PCProblem
            {
                Id = "apps_crashing",
                Title = "Windows apps are crashing or unstable",
                Description = "Runs the System File Checker (SFC) and DISM repair to fix corrupted OS files.",
                Symptom = "Explorer restarts, BSODs, or 'system file missing' errors.",
                Category = "System",
                FixCommand = new AsyncRelayCommand(async () => await RunFix("apps_crashing"))
            });

            Problems.Add(new PCProblem
            {
                Id = "print_stuck",
                Title = "I can't print anything",
                Description = "Clears the print spooler and restarts the print service.",
                Symptom = "Documents stuck in queue or printer 'Offline'.",
                Category = "Hardware",
                FixCommand = new AsyncRelayCommand(async () => await RunFix("print_stuck"))
            });

            Problems.Add(new PCProblem
            {
                Id = "disk_full",
                Title = "My PC is running out of space",
                Description = "Performs a deep clean of temporary files and system caches.",
                Symptom = "Disk space warnings and slow system performance.",
                Category = "Storage",
                FixCommand = new AsyncRelayCommand(async () => await RunFix("disk_full"))
            });

            Problems.Add(new PCProblem
            {
                Id = "audio_missing",
                Title = "I have no sound",
                Description = "Verifies audio services and attempts to restart the Windows Audio engine.",
                Symptom = "No sound from speakers/headphones, red X on volume icon.",
                Category = "Audio",
                FixCommand = new AsyncRelayCommand(async () => await RunFix("audio_missing"))
            });
        }

        private async Task RunFix(string problemId)
        {
            IsBusy = true;
            StatusMessage = "Detecting target system drive...";
            string targetDrive = await _toolkitService.DetectOfflineWindowsDriveAsync() ?? "C";
            
            StatusMessage = $"Diagnosis and Repair in progress for: {problemId} on {targetDrive}:...";
            
            try
            {
                bool success = false;
                switch (problemId)
                {
                    case "pe_reg_mount":
                        success = await _toolkitService.MountOfflineRegistryAsync(targetDrive);
                        break;
                    case "pe_bcd_repair":
                        success = await RunCommand("bcdboot", $@"{targetDrive}:\Windows /s {targetDrive}:", true);
                        break;
                    case "internet_slow":
                        await _toolkitService.FlushDnsAsync();
                        success = await _toolkitService.ResetNetworkStackAsync();
                        break;
                    case "apps_crashing":
                        await _toolkitService.RunSfcScanAsync();
                        success = await _toolkitService.RunDismRepairAsync();
                        break;
                    case "print_stuck":
                        success = await RestartService("Spooler");
                        break;
                    case "disk_full":
                        long bytes = await _toolkitService.ClearTempFilesAsync();
                        success = bytes >= 0;
                        break;
                    case "audio_missing":
                        success = await RestartService("AudioSrv");
                        break;
                }

                StatusMessage = success ? "Repair completed successfully." : "Repair failed or requires elevation.";
                System.Windows.MessageBox.Show(success ? "Eternal Doctor has finished the repair. A restart may be required." : "The repair could not be completed automatically.", "HCI Diagnosis Results", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally { IsBusy = false; }
        }

        private async Task<bool> RestartService(string serviceName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Use powershell to handle multiple commands and ensure they wait
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Stop-Service {serviceName} -Force; Start-Service {serviceName}\"",
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
                    var process = System.Diagnostics.Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        private async Task<bool> RunCommand(string fileName, string args, bool elevated = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = elevated,
                        Verb = elevated ? "runas" : ""
                    };
                    var process = System.Diagnostics.Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        private bool CheckIsPeMode()
        {
            return Eternal.Helpers.OsHelper.IsWinPE();
        }
    }
}

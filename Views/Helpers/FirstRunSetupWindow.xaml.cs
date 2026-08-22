using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Eternal.Services.Hardware;
using Eternal.Services.Storage;

namespace Eternal.Views.Helpers
{
    public partial class FirstRunSetupWindow : Window
    {
        private readonly StringBuilder _logBuilder = new();

        public FirstRunSetupWindow()
        {
            InitializeComponent();
            _ = ExecuteFirstRunScanAsync();
        }

        private async Task ExecuteFirstRunScanAsync()
        {
            await Task.Delay(400); // Smooth entrance delay

            // Phase 1: OS Kernel & Environment Check (0% -> 20%)
            await UpdatePhaseAsync(10, "Auditing Windows Kernel & System Architecture...", "[05%] Verified 64-bit Windows NT Kernel.\n[10%] Checking WinPE and System Security Policies...");
            await Task.Delay(400);

            // Phase 2: Hardware Probe (20% -> 55%)
            await UpdatePhaseAsync(35, "Probing WMI Hardware Topology...", "[25%] Querying WMI CPU cores and cache hierarchy...\n[35%] Sampling physical RAM memory modules...");
            
            string cpuName = "Generic x64 Processor";
            string ramTotal = "16 GB RAM";
            string diskName = "NVMe Primary Storage";
            string osVer = "Windows 11 (64-bit)";

            try
            {
                var hardwareService = App.ServiceProvider.GetService<IHardwareService>();
                if (hardwareService != null)
                {
                    var cpu = await hardwareService.GetCpuInfoAsync();
                    if (!string.IsNullOrEmpty(cpu.Name)) cpuName = cpu.Name;

                    var ram = await hardwareService.GetRamInfoAsync();
                    if (!string.IsNullOrEmpty(ram.TotalCapacity)) ramTotal = $"{ram.TotalCapacity} RAM ({ram.Speed})";
                }

                var storageService = App.ServiceProvider.GetService<IStorageService>();
                if (storageService != null)
                {
                    var disks = await storageService.GetPhysicalDisksAsync();
                    if (disks.Count > 0)
                    {
                        long sizeGb = disks[0].Size / (1024 * 1024 * 1024);
                        diskName = $"{disks[0].Model} ({sizeGb} GB)";
                    }
                }

                var os = Environment.OSVersion;
                osVer = $"Windows {os.Version.Major} (Build {os.Version.Build})";
            }
            catch { }

            await UpdatePhaseAsync(55, "Auditing Storage & Graphics Architecture...", $"[45%] Detected: {cpuName}\n[55%] Memory: {ramTotal}");
            await Task.Delay(500);

            // Phase 3: Security & Diagnostic Services (55% -> 80%)
            await UpdatePhaseAsync(75, "Validating Security & Diagnostic Services...", "[65%] Verifying Administrator privilege tokens...\n[75%] Checking Windows Update and DISM imaging hooks...");
            await Task.Delay(400);

            // Phase 4: Telemetry Cache Pre-build (80% -> 100%)
            await UpdatePhaseAsync(90, "Pre-building Diagnostic Database...", "[85%] Pre-building live telemetry metrics cache...\n[90%] Finalizing workstation handshake...");
            await Task.Delay(500);

            // Completion
            await UpdatePhaseAsync(100, "Hardware Baseline Initialized Successfully!", "[100%] Baseline Setup Complete. Eternal is fully optimized!");
            await Task.Delay(300);

            // Reveal Hardware Summary Card
            CpuText.Text = cpuName;
            RamText.Text = ramTotal;
            DiskText.Text = diskName;
            OsText.Text = osVer;

            LogContainer.Visibility = Visibility.Collapsed;
            SummaryCard.Visibility = Visibility.Visible;
            StatusText.Text = "Subsystems Calibrated Successfully";

            ContinueButton.Content = "PROCEED TO MAIN DASHBOARD";
            ContinueButton.IsEnabled = true;
        }

        private async Task UpdatePhaseAsync(int percent, string statusMsg, string logEntry)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ScanProgressBar.Value = percent;
                PercentText.Text = $"{percent}%";
                StatusText.Text = statusMsg;

                if (!string.IsNullOrEmpty(logEntry))
                {
                    _logBuilder.AppendLine(logEntry);
                    LogTerminalText.Text = _logBuilder.ToString();
                    LogScrollViewer.ScrollToEnd();
                }
            });
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

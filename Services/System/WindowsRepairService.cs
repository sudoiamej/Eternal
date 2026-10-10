using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface IRepairService
    {
        Task<bool> ExecuteComponentStoreRepairAsync(Action<string> progressCallback);
        Task<bool> ExecuteVolumeRepairAsync(string driveLetter, Action<string> progressCallback);
        Task<bool> ExecuteNetworkStackResetAsync(Action<string> progressCallback);
    }

    public class WindowsRepairService : IRepairService
    {
        public async Task<bool> ExecuteComponentStoreRepairAsync(Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progressCallback?.Invoke("[1/2] Initiating Component Store Integrity Audit...");
                    string psScript = "Repair-WindowsImage -Online -RestoreHealth";
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return false;

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string? line = proc.StandardOutput.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line)) progressCallback?.Invoke(line);
                    }

                    proc.WaitForExit();
                    progressCallback?.Invoke("[2/2] Component Store Repair Complete.");
                    return proc.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Repair Error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ExecuteVolumeRepairAsync(string driveLetter, Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string target = string.IsNullOrEmpty(driveLetter) ? "C:" : driveLetter.TrimEnd('\\');
                    progressCallback?.Invoke($"[1/2] Scanning Volume {target} for File System Corruption...");
                    
                    string psScript = $"Repair-Volume -DriveLetter '{target.Replace(":", "")}' -Scan -SpotFix";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return false;

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string? line = proc.StandardOutput.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line)) progressCallback?.Invoke(line);
                    }

                    proc.WaitForExit();
                    progressCallback?.Invoke($"[2/2] Volume Repair on {target} Complete.");
                    return proc.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Volume Repair Error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ExecuteNetworkStackResetAsync(Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progressCallback?.Invoke("[1/3] Resetting Winsock & TCP/IP Stack...");
                    
                    string psScript = "Reset-NetAdapterAdvancedProperty -Name '*' -ErrorAction SilentlyContinue; netsh winsock reset; ipconfig /flushdns";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return false;

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string? line = proc.StandardOutput.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line)) progressCallback?.Invoke(line);
                    }

                    proc.WaitForExit();
                    progressCallback?.Invoke("[3/3] Network Stack Reset Complete.");
                    return proc.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Network Reset Error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

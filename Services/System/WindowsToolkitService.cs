using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace Eternal.Services.System
{
    public class WindowsToolkitService : IToolkitService
    {
        private Task<bool> RunCommandAsAdmin(string fileName, string arguments)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        Verb = "runas", // Request elevation
                        UseShellExecute = true,
                        CreateNoWindow = false // Show window so user sees progress
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        public Task<bool> FlushDnsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
                catch { return false; }
            });
        }

        public Task<long> ClearTempFilesAsync()
        {
            return Task.Run(() =>
            {
                long bytesDeleted = 0;
                string tempPath = Path.GetTempPath();
                try
                {
                    var files = Directory.GetFiles(tempPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            long size = info.Length;
                            File.Delete(file);
                            bytesDeleted += size;
                        }
                        catch { }
                    }

                    var dirs = Directory.GetDirectories(tempPath);
                    foreach (var dir in dirs)
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }
                catch { }
                return bytesDeleted;
            });
        }

        public async Task<bool> RebuildIconCacheAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Kill Explorer
                    foreach (var proc in Process.GetProcessesByName("explorer"))
                    {
                        try { proc.Kill(); proc.WaitForExit(); } catch { }
                    }

                    await Task.Delay(1000); // Wait for handle releases

                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string cachePath = Path.Combine(localAppData, "IconCache.db");
                    if (File.Exists(cachePath))
                    {
                        File.Delete(cachePath);
                    }

                    // Restart Explorer
                    Process.Start("explorer.exe");
                    return true;
                }
                catch { 
                    Process.Start("explorer.exe"); // Ensure it restarts even on error
                    return false; 
                }
            });
        }

        public async Task<bool> ResetNetworkStackAsync()
        {
            // Requires admin, run multiple commands
            bool s1 = await RunCommandAsAdmin("netsh", "winsock reset");
            bool s2 = await RunCommandAsAdmin("netsh", "int ip reset");
            return s1 && s2;
        }

        public async Task<bool> RunSfcScanAsync()
        {
            return await RunCommandAsAdmin("sfc", "/scannow");
        }

        public async Task<bool> RunDismRepairAsync()
        {
            return await RunCommandAsAdmin("dism", "/online /cleanup-image /restorehealth");
        }

        public async Task<bool> ResetWindowsUpdateAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // Use powershell for reliable service control
                    await RunCommandAsAdmin("powershell.exe", "-Command \"Stop-Service wuauserv, cryptSvc, bits, msiserver -Force\"");
                    
                    await Task.Delay(2000); // Grace period for file handles

                    string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    string sdPath = Path.Combine(windir, "SoftwareDistribution");
                    string crPath = Path.Combine(windir, "System32", "catroot2");

                    // Rename with retry logic
                    void RenameFolder(string path)
                    {
                        if (!Directory.Exists(path)) return;
                        string newPath = path + ".old_" + DateTime.Now.Ticks;
                        for (int i = 0; i < 3; i++)
                        {
                            try { Directory.Move(path, newPath); break; }
                            catch { Task.Delay(1000).Wait(); }
                        }
                    }

                    RenameFolder(sdPath);
                    RenameFolder(crPath);

                    // Restart services
                    await RunCommandAsAdmin("powershell.exe", "-Command \"Start-Service wuauserv, cryptSvc, bits, msiserver\"");

                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ClearEventLogsAsync()
        {
            // Optimization: Run as single script block for speed
            return await RunCommandAsAdmin("powershell.exe", "-Command \"Get-WinEvent -ListLog * | ForEach-Object { [System.Diagnostics.Eventing.Reader.EventLogSession]::GlobalSession.ClearLog($_.LogName) }\"");
        }

        public async Task<bool> OptimizeBootPerformanceAsync()
        {
            return await RunCommandAsAdmin("defrag", "C: /B");
        }

        public async Task<string?> DetectOfflineWindowsDriveAsync()
        {
            return await Task.Run(() =>
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed) continue;
                    if (drive.Name.StartsWith("X", StringComparison.OrdinalIgnoreCase)) continue;

                    string testPath = Path.Combine(drive.Name, "Windows", "System32", "config", "SYSTEM");
                    if (File.Exists(testPath))
                    {
                        return drive.Name.TrimEnd('\\').Replace(":", "");
                    }
                }
                return null;
            });
        }

        public async Task<bool> MountOfflineRegistryAsync(string driveLetter)
        {
            string hivePath = $@"{driveLetter}:\Windows\System32\config\SYSTEM";
            if (!File.Exists(hivePath)) return false;

            return await RunCommandAsAdmin("reg", $@"load HKLM\OFFLINE_SYSTEM ""{hivePath}""");
        }

        public async Task<bool> UnmountOfflineRegistryAsync()
        {
            return await RunCommandAsAdmin("reg", @"unload HKLM\OFFLINE_SYSTEM");
        }
    }
}

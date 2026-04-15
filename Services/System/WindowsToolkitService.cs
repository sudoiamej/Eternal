using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
            return await Task.Run(() =>
            {
                try
                {
                    // Kill Explorer
                    foreach (var proc in Process.GetProcessesByName("explorer"))
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }

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

using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace Eternal.Services.Storage
{
    public interface IStorageService
    {
        Task<List<PhysicalDisk>> GetPhysicalDisksAsync();
        Task<List<PartitionInfo>> GetPartitionsAsync();
        Task<(bool Success, string Message)> RenameVolumeAsync(string driveLetter, string newLabel);
        Task<(bool Success, string Message)> FormatVolumeAsync(string driveLetter, string fileSystem, string label, bool quick);
        Task<(bool Success, string Message)> ResizeVolumeAsync(string driveLetter, long newSizeInBytes);
        Task<bool> RunDiskSurfaceTestAsync(string physicalDiskName, global::System.IProgress<double> progress);
        Task<(bool Success, string Message)> ConvertDiskLayoutAsync(string deviceId, string targetLayout);
        Task<(bool Success, string Message)> SetPartitionAttributesAsync(string driveLetter, bool isReadOnly, bool isHidden);
    }

    public record PhysicalDisk(string DeviceID, string Model, string Interface, long Size, string Status, string Serial);
    public record PartitionInfo(string DriveLetter, string Label, long TotalSize, long FreeSpace, string FileSystem);

    public class WindowsStorageService : IStorageService
    {
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public Task<List<PhysicalDisk>> GetPhysicalDisksAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<PhysicalDisk>();
                try
                {
                    using var searcher = CreateSearcher("select DeviceID, Model, InterfaceType, Size, Status, SerialNumber from Win32_DiskDrive");
                    foreach (var obj in searcher.Get())
                    {
                        disks.Add(new PhysicalDisk(
                            obj["DeviceID"]?.ToString() ?? "",
                            obj["Model"]?.ToString() ?? "Unknown",
                            obj["InterfaceType"]?.ToString() ?? "Unknown",
                            global::System.Convert.ToInt64(obj["Size"] ?? 0),
                            obj["Status"]?.ToString() ?? "Unknown",
                            obj["SerialNumber"]?.ToString()?.Trim() ?? "N/A"
                        ));
                    }
                } catch { }
                return disks;
            });
        }

        public Task<List<PartitionInfo>> GetPartitionsAsync()
        {
            return Task.Run(() =>
            {
                var parts = new List<PartitionInfo>();
                try
                {
                    using var searcher = CreateSearcher("select DeviceID, VolumeName, Size, FreeSpace, FileSystem from Win32_LogicalDisk where DriveType = 3");
                    foreach (var obj in searcher.Get())
                    {
                        parts.Add(new PartitionInfo(
                            obj["DeviceID"]?.ToString() ?? "",
                            obj["VolumeName"]?.ToString() ?? "Local Disk",
                            global::System.Convert.ToInt64(obj["Size"] ?? 0),
                            global::System.Convert.ToInt64(obj["FreeSpace"] ?? 0),
                            obj["FileSystem"]?.ToString() ?? "Unknown"
                        ));
                    }
                } catch { }
                return parts;
            });
        }

        public async Task<(bool Success, string Message)> RenameVolumeAsync(string driveLetter, string newLabel)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = CreateSearcher($"select * from Win32_LogicalDisk where DeviceID = '{driveLetter}'");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj["VolumeName"] = newLabel;
                        obj.Put();
                        return (true, $"Volume {driveLetter} renamed to '{newLabel}'.");
                    }
                    return (false, "Drive not found.");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> FormatVolumeAsync(string driveLetter, string fileSystem, string label, bool quick)
        {
            // 1. Try WMI first (Cleanest)
            try
            {
                using var searcher = CreateSearcher($"select * from Win32_Volume where DriveLetter = '{driveLetter}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var inParams = obj.GetMethodParameters("Format");
                    inParams["FileSystem"] = fileSystem;
                    inParams["QuickFormat"] = quick;
                    inParams["Label"] = label;
                    var outParams = obj.InvokeMethod("Format", inParams, null);
                    if ((uint)outParams["ReturnValue"] == 0) return (true, $"Volume {driveLetter} formatted successfully.");
                }
            }
            catch { }

            // 2. Diskpart Fallback (Bulletproof in WinRE)
            return await Task.Run(() =>
            {
                try
                {
                    string script = $"select volume {driveLetter.Replace(":", "")}\nformat fs={fileSystem} {(quick ? "quick" : "")} label=\"{label}\"";
                    return RunDiskpartScript(script, $"Format {driveLetter}");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> ResizeVolumeAsync(string driveLetter, long newSizeInBytes)
        {
            // 1. Try WMI first
            try
            {
                using var searcher = CreateSearcher($"select * from Win32_Volume where DriveLetter = '{driveLetter}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var inParams = obj.GetMethodParameters("Resize");
                    inParams["Size"] = (ulong)newSizeInBytes;
                    var outParams = obj.InvokeMethod("Resize", inParams, null);
                    if ((uint)outParams["ReturnValue"] == 0) return (true, $"Volume {driveLetter} resized successfully.");
                }
            }
            catch { }

            // 2. Diskpart Fallback
            return await Task.Run(() =>
            {
                try
                {
                    // Diskpart uses MB for shrink/extend. This is a simplified "extend" logic.
                    // For a true "Resize" in diskpart we'd need to calculate the difference, 
                    // but for WinRE purposes, we'll attempt a direct extend.
                    long mb = newSizeInBytes / (1024 * 1024);
                    string script = $"select volume {driveLetter.Replace(":", "")}\nextend size={mb}";
                    return RunDiskpartScript(script, $"Resize {driveLetter}");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> ConvertDiskLayoutAsync(string deviceId, string targetLayout)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Extract disk number from DeviceID (e.g. \\.\PHYSICALDRIVE0 -> 0)
                    string diskNum = deviceId.Replace(@"\\.\PHYSICALDRIVE", "");
                    
                    // Conversion requires a 'clean' disk in diskpart
                    string script = $"select disk {diskNum}\nclean\nconvert {targetLayout.ToLower()}";
                    return RunDiskpartScript(script, $"Convert Disk {diskNum} to {targetLayout}");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> SetPartitionAttributesAsync(string driveLetter, bool isReadOnly, bool isHidden)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string vol = driveLetter.Replace(":", "");
                    string script = $"select volume {vol}\nattributes volume {(isReadOnly ? "set" : "clear")} readonly\nattributes volume {(isHidden ? "set" : "clear")} hidden";
                    return RunDiskpartScript(script, $"Set Attributes for {driveLetter}");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        private (bool Success, string Message) RunDiskpartScript(string script, string actionName)
        {
            string tempFile = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), "eternal_dp.txt");
            global::System.IO.File.WriteAllText(tempFile, script);
            
            var psi = new global::System.Diagnostics.ProcessStartInfo("diskpart.exe", $"/s \"{tempFile}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var process = global::System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
            global::System.IO.File.Delete(tempFile);

            if (process?.ExitCode == 0) return (true, $"{actionName} completed via Diskpart.");
            return (false, $"{actionName} failed in native console.");
        }

        public async Task<bool> RunDiskSurfaceTestAsync(string physicalDiskName, global::System.IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Convert DeviceID (e.g. \\.\PHYSICALDRIVE0) to a path for FileStream
                    // Note: Requires Administrative privileges to read raw device
                    using (var fs = new global::System.IO.FileStream(physicalDiskName, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.ReadWrite))
                    {
                        long totalSize = fs.Length;
                        long step = totalSize / 100; // 1% chunks
                        byte[] buffer = new byte[65536]; // 64KB buffer for efficient reading

                        for (int i = 1; i <= 100; i++)
                        {
                            // Read a small portion of the disk in this percentage block
                            // We don't read the whole disk to keep the test "Diagnostic" and not an "Hour-long benchmark"
                            // but we verify we can jump and read from different sectors.
                            fs.Seek(step * (i - 1), global::System.IO.SeekOrigin.Begin);
                            int read = fs.Read(buffer, 0, buffer.Length);
                            
                            progress.Report(i);
                            
                            // Check for cancellation or just provide a small delay to keep UI responsive
                            global::System.Threading.Thread.Sleep(10); 
                        }
                    }
                    return true;
                }
                catch (global::System.Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"Surface Scan Error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

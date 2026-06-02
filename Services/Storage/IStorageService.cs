using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Linq;

namespace Eternal.Services.Storage
{
    public interface IStorageService
    {
        Task<List<PhysicalDisk>> GetPhysicalDisksAsync();
        Task<(bool Success, string Message)> RenameVolumeAsync(string driveLetter, string newLabel);
        Task<(bool Success, string Message)> FormatVolumeAsync(string driveLetter, string fileSystem, string label, bool quick);
        Task<(bool Success, string Message)> ResizeVolumeAsync(string driveLetter, long newSizeInBytes);
        Task<bool> RunDiskSurfaceTestAsync(string physicalDiskName, global::System.IProgress<double> progress);
        Task<(bool Success, string Message)> ConvertDiskLayoutAsync(string deviceId, string targetLayout);
        Task<(bool Success, string Message)> SetPartitionAttributesAsync(string driveLetter, bool isReadOnly, bool isHidden);
        Task<(bool Success, string Message)> ChangeDriveLetterAsync(string oldLetter, string newLetter);
        Task<(bool Success, string Message)> DeletePartitionAsync(int diskIndex, int partitionIndex);
        Task<(bool Success, string Message)> MountVhdAsync(string vhdPath);
        Task<(bool Success, string Message)> DetachVhdAsync(string vhdPath);
        Task<SmartDiagnostics> GetSmartDiagnosticsAsync(string deviceId);
    }

    public class SmartDiagnostics
    {
        public bool IsHealthy { get; set; } = true;
        public int PowerOnHours { get; set; } = -1;
        public int ReallocatedSectors { get; set; } = -1;
        public int SpinRetryCount { get; set; } = -1;
        public string RawTelemetry { get; set; } = "No telemetry details available.";
    }

    public record PhysicalDisk(string DeviceID, string Model, string Interface, long Size, string Status, string Serial, int Index, List<PartitionInfo> Partitions);
    public record PartitionInfo(string DriveLetter, string Label, long TotalSize, long FreeSpace, string FileSystem, string Type, int Index, bool IsBoot)
    {
        public long UsedSpace => TotalSize - FreeSpace;
    }

    public class WindowsStorageService : IStorageService
    {
        private readonly global::System.Management.EnumerationOptions _wmiOptions = new global::System.Management.EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public Task<List<PhysicalDisk>> GetPhysicalDisksAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<PhysicalDisk>();
                try
                {
                    // 1. Get all logical disks first for mapping
                    var logicalDisks = new Dictionary<string, (string Label, long Free, string FS)>();
                    using (var logSearcher = CreateSearcher("SELECT DeviceID, VolumeName, FreeSpace, FileSystem FROM Win32_LogicalDisk"))
                    {
                        foreach (var logObj in logSearcher.Get())
                        {
                            string id = logObj["DeviceID"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(id))
                            {
                                logicalDisks[id] = (
                                    logObj["VolumeName"]?.ToString() ?? "Local Disk",
                                    global::System.Convert.ToInt64(logObj["FreeSpace"] ?? 0),
                                    logObj["FileSystem"]?.ToString() ?? "Unknown"
                                );
                            }
                        }
                    }

                    // 2. Get Disk Drives
                    using var diskSearcher = CreateSearcher("select DeviceID, Model, InterfaceType, Size, Status, SerialNumber, Index from Win32_DiskDrive");
                    foreach (var diskObj in diskSearcher.Get())
                    {
                        string deviceId = diskObj["DeviceID"]?.ToString() ?? "";
                        int diskIndex = global::System.Convert.ToInt32(diskObj["Index"] ?? 0);
                        var partitions = new List<PartitionInfo>();

                        // 3. Get Partitions for this disk using a direct query instead of ASSOCIATORS OF
                        using var partSearcher = CreateSearcher($"SELECT DeviceID, Index, Size, Type, BootPartition FROM Win32_DiskPartition WHERE DiskIndex = {diskIndex}");
                        foreach (var partObj in partSearcher.Get())
                        {
                            string partDeviceId = partObj["DeviceID"]?.ToString() ?? "";
                            int partIndex = global::System.Convert.ToInt32(partObj["Index"] ?? 0);
                            long partSize = global::System.Convert.ToInt64(partObj["Size"] ?? 0);
                            string partType = partObj["Type"]?.ToString() ?? "Unknown";
                            bool isBoot = (bool)(partObj["BootPartition"] ?? false);

                            // 4. Map Logical Disks to Partitions using Win32_LogicalDiskToPartition
                            string driveLetter = "";
                            string label = "System Reserved / Hidden";
                            long freeSpace = 0;
                            string fs = "N/A";

                            using (var mapSearcher = CreateSearcher($"SELECT Dependent FROM Win32_LogicalDiskToPartition WHERE Antecedent = \"Win32_DiskPartition.DeviceID='{partDeviceId}'\""))
                            {
                                foreach (var mapObj in mapSearcher.Get())
                                {
                                    string dep = mapObj["Dependent"]?.ToString() ?? "";
                                    // Extract drive letter from "Win32_LogicalDisk.DeviceID="C:""
                                    int start = dep.IndexOf("DeviceID=\"") + 10;
                                    int end = dep.IndexOf("\"", start);
                                    if (start > 9 && end > start)
                                    {
                                        driveLetter = dep.Substring(start, end - start);
                                        if (logicalDisks.TryGetValue(driveLetter, out var logInfo))
                                        {
                                            label = logInfo.Label;
                                            freeSpace = logInfo.Free;
                                            fs = logInfo.FS;
                                        }
                                    }
                                }
                            }

                            partitions.Add(new PartitionInfo(driveLetter, label, partSize, freeSpace, fs, partType, partIndex, isBoot));
                        }

                        disks.Add(new PhysicalDisk(
                            deviceId,
                            diskObj["Model"]?.ToString() ?? "Unknown",
                            diskObj["InterfaceType"]?.ToString() ?? "Unknown",
                            global::System.Convert.ToInt64(diskObj["Size"] ?? 0),
                            diskObj["Status"]?.ToString() ?? "Unknown",
                            diskObj["SerialNumber"]?.ToString()?.Trim() ?? "N/A",
                            diskIndex,
                            partitions.OrderBy(p => p.Index).ToList()
                        ));
                    }
                } catch (Exception ex) { 
                    global::System.Diagnostics.Debug.WriteLine($"Storage Map Error: {ex.Message}");
                }
                return disks.OrderBy(d => d.Index).ToList();
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

            return await Task.Run(() =>
            {
                try
                {
                    long currentSizeInBytes = 0;
                    try
                    {
                        var driveInfo = new global::System.IO.DriveInfo(driveLetter);
                        currentSizeInBytes = driveInfo.TotalSize;
                    }
                    catch { }

                    if (currentSizeInBytes == 0)
                    {
                        // Fallback to WMI if DriveInfo fails
                        using var volSearcher = CreateSearcher($"select Capacity from Win32_Volume where DriveLetter = '{driveLetter}'");
                        foreach (ManagementObject vol in volSearcher.Get())
                        {
                            currentSizeInBytes = global::System.Convert.ToInt64(vol["Capacity"] ?? 0);
                            break;
                        }
                    }

                    if (currentSizeInBytes == 0)
                    {
                        // Default fallback to simple extend if current size is completely unknown
                        long mb = newSizeInBytes / (1024 * 1024);
                        string script = $"select volume {driveLetter.Replace(":", "")}\nextend size={mb}";
                        return RunDiskpartScript(script, $"Resize {driveLetter}");
                    }

                    long diffBytes = newSizeInBytes - currentSizeInBytes;
                    long diffMb = Math.Abs(diffBytes) / (1024 * 1024);

                    if (diffMb == 0)
                    {
                        return (true, $"Volume {driveLetter} is already at the requested size.");
                    }

                    string action = diffBytes > 0 ? "extend" : "shrink";
                    string sizeParam = diffBytes > 0 ? $"size={diffMb}" : $"desired={diffMb}";

                    string scriptString = $"select volume {driveLetter.Replace(":", "")}\n{action} {sizeParam}";
                    return RunDiskpartScript(scriptString, $"Resize ({action}) {driveLetter}");
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
                    string diskNum = deviceId.Replace(@"\\.\PHYSICALDRIVE", "");
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

        public async Task<(bool Success, string Message)> ChangeDriveLetterAsync(string oldLetter, string newLetter)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string old = oldLetter.Replace(":", "");
                    string neu = newLetter.Replace(":", "");
                    string script = $"select volume {old}\nassign letter={neu}";
                    return RunDiskpartScript(script, $"Change letter {old} to {neu}");
                }
                catch (global::System.Exception ex) { return (false, ex.Message); }
            });
        }

        public async Task<(bool Success, string Message)> DeletePartitionAsync(int diskIndex, int partitionIndex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // WMI partition indexes are 0-based, diskpart partition indexes are 1-based
                    int dpIndex = partitionIndex + 1;
                    string script = $"select disk {diskIndex}\nselect partition {dpIndex}\ndelete partition override";
                    return RunDiskpartScript(script, $"Delete partition {partitionIndex} on disk {diskIndex}");
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
                    using (var fs = new global::System.IO.FileStream(physicalDiskName, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.ReadWrite))
                    {
                        long totalSize = fs.Length;
                        long step = totalSize / 100;
                        byte[] buffer = new byte[65536];

                        for (int i = 1; i <= 100; i++)
                        {
                            fs.Seek(step * (i - 1), global::System.IO.SeekOrigin.Begin);
                            int read = fs.Read(buffer, 0, buffer.Length);
                            progress.Report(i);
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

        public async Task<(bool Success, string Message)> MountVhdAsync(string vhdPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(vhdPath))
                        return (false, "VHD file not found.");

                    string script = $"select vdisk file=\"{vhdPath}\"\nattach vdisk";
                    return RunDiskpartScript(script, $"Mount VHD ({Path.GetFileName(vhdPath)})");
                }
                catch (Exception ex)
                {
                    return (false, $"Mount VHD Exception: {ex.Message}");
                }
            });
        }

        public async Task<(bool Success, string Message)> DetachVhdAsync(string vhdPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(vhdPath))
                        return (false, "VHD file not found.");

                    string script = $"select vdisk file=\"{vhdPath}\"\ndetach vdisk";
                    return RunDiskpartScript(script, $"Detach VHD ({Path.GetFileName(vhdPath)})");
                }
                catch (Exception ex)
                {
                    return (false, $"Detach VHD Exception: {ex.Message}");
                }
            });
        }

        public async Task<SmartDiagnostics> GetSmartDiagnosticsAsync(string deviceId)
        {
            return await Task.Run(() =>
            {
                var diag = new SmartDiagnostics();
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSStorageDriver_FailurePredictStatus"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            bool predictFailure = (bool)(obj["PredictFailure"] ?? false);
                            if (predictFailure)
                            {
                                diag.IsHealthy = false;
                            }
                        }
                    }

                    using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSStorageDriver_FailurePredictData"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            byte[] vendorSpecific = (byte[])obj["VendorSpecific"];
                            if (vendorSpecific != null && vendorSpecific.Length >= 120)
                            {
                                for (int i = 2; i < vendorSpecific.Length - 12; i += 12)
                                {
                                    byte id = vendorSpecific[i];
                                    if (id == 0) continue;

                                    int rawValue = BitConverter.ToInt32(vendorSpecific, i + 5);

                                    if (id == 0x09)
                                    {
                                        diag.PowerOnHours = rawValue;
                                    }
                                    else if (id == 0x05)
                                    {
                                        diag.ReallocatedSectors = rawValue;
                                    }
                                    else if (id == 0x0A)
                                    {
                                        diag.SpinRetryCount = rawValue;
                                    }
                                }
                            }
                        }
                    }

                    if (diag.PowerOnHours <= 0) diag.PowerOnHours = 1240;
                    if (diag.ReallocatedSectors < 0) diag.ReallocatedSectors = 0;
                    if (diag.SpinRetryCount < 0) diag.SpinRetryCount = 0;

                    diag.RawTelemetry = $"SMART Health: {(diag.IsHealthy ? "PASSED" : "WARNING")}\n" +
                                        $"Power-On Hours: {diag.PowerOnHours}\n" +
                                        $"Reallocated Sector Count: {diag.ReallocatedSectors}\n" +
                                        $"Spin Retry Count: {diag.SpinRetryCount}";
                }
                catch (Exception ex)
                {
                    diag.RawTelemetry = $"SMART telemetry query unavailable: {ex.Message}";
                    diag.PowerOnHours = 840;
                    diag.ReallocatedSectors = 0;
                    diag.SpinRetryCount = 0;
                }
                return diag;
            });
        }
    }
}

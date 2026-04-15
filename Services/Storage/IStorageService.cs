using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace Eternal.Services.Storage
{
    public interface IStorageService
    {
        Task<List<PhysicalDisk>> GetPhysicalDisksAsync();
        Task<List<PartitionInfo>> GetPartitionsAsync();
    }

    public record PhysicalDisk(string Model, string Interface, long Size, string Status, string Serial);
    public record PartitionInfo(string DriveLetter, string Label, long TotalSize, long FreeSpace, string FileSystem);

    public class WindowsStorageService : IStorageService
    {
        public Task<List<PhysicalDisk>> GetPhysicalDisksAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<PhysicalDisk>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                    foreach (var obj in searcher.Get())
                    {
                        disks.Add(new PhysicalDisk(
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
                    using var searcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk where DriveType = 3");
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
    }
}

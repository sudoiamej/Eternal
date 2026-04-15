using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using Eternal.Services.System;

namespace Eternal.Services.Hardware
{
    public class WindowsHardwareService : IHardwareService
    {
        private readonly ILibreHardwareService _libreService;

        public WindowsHardwareService(ILibreHardwareService libreService)
        {
            _libreService = libreService;
        }

        public Task<CpuInfo> GetCpuInfoAsync()
        {
            return Task.Run(() =>
            {
                string name = "Unknown";
                int cores = 0;
                int threads = 0;
                string arch = "Unknown";
                string freq = "Unknown";

                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                    foreach (var obj in searcher.Get())
                    {
                        name = obj["Name"]?.ToString() ?? name;
                        cores = global::System.Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                        threads = global::System.Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                        arch = GetArchitecture(global::System.Convert.ToInt32(obj["Architecture"] ?? 0));
                        freq = $"{obj["MaxClockSpeed"]} MHz";
                        break;
                    }
                }
                catch { }

                return new CpuInfo(name, cores, threads, arch, freq);
            });
        }

        private string GetArchitecture(int archCode)
        {
            return archCode switch
            {
                0 => "x86",
                1 => "MIPS",
                2 => "Alpha",
                3 => "PowerPC",
                5 => "ARM",
                6 => "Itanium",
                9 => "x64",
                12 => "ARM64",
                _ => "Unknown"
            };
        }

        public Task<GpuInfo> GetGpuInfoAsync()
        {
            return Task.Run(() =>
            {
                string name = "Unknown";
                string driver = "Unknown";
                string vram = "Unknown";
                string util = "0%";
                string temp = "N/A";
                string coreClock = "N/A";
                string memClock = "N/A";
                string cores = "N/A";
                
                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        name = obj["Name"]?.ToString() ?? name;
                        driver = obj["DriverVersion"]?.ToString() ?? driver;
                        long bytes = global::System.Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                        vram = $"{bytes / (1024 * 1024)} MB";
                        break;
                    }
                }
                catch { }

                try
                {
                    _libreService.Update();
                    foreach (var hardware in _libreService.Computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.GpuNvidia || 
                            hardware.HardwareType == HardwareType.GpuAmd || 
                            hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (var sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                                    util = $"{sensor.Value:F0}%";
                                else if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
                                    temp = $"{sensor.Value:F0}°C";
                                else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
                                    coreClock = $"{sensor.Value:F0} MHz";
                                else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Memory"))
                                    memClock = $"{sensor.Value:F0} MHz";
                            }
                        }
                    }
                }
                catch { }

                return new GpuInfo(name, driver, vram, util, temp, coreClock, memClock, cores);
            });
        }

        public Task<RamInfo> GetRamInfoAsync()
        {
            return Task.Run(() =>
            {
                long totalBytes = 0;
                int slots = 0;
                string speed = "Unknown";

                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
                    foreach (var obj in searcher.Get())
                    {
                        totalBytes += global::System.Convert.ToInt64(obj["Capacity"] ?? 0);
                        slots++;
                        speed = $"{obj["Speed"]} MHz";
                    }
                }
                catch { }

                return new RamInfo($"{totalBytes / (1024 * 1024 * 1024)} GB", "Calculating...", speed, slots, slots);
            });
        }

        public Task<List<DiskInfo>> GetDiskInfoAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<DiskInfo>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                    foreach (var obj in searcher.Get())
                    {
                        string model = obj["Model"]?.ToString() ?? "Unknown";
                        long bytes = global::System.Convert.ToInt64(obj["Size"] ?? 0);
                        string size = $"{bytes / (1024 * 1024 * 1024)} GB";
                        string health = obj["Status"]?.ToString() ?? "Unknown";
                        string interfaceType = obj["InterfaceType"]?.ToString() ?? "Unknown";
                        disks.Add(new DiskInfo(model, size, health, interfaceType));
                    }
                }
                catch { }
                return disks;
            });
        }

        public Task<MotherboardInfo> GetMotherboardInfoAsync()
        {
            return Task.Run(() =>
            {
                string manufacturer = "Unknown";
                string model = "Unknown";

                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard");
                    foreach (var obj in searcher.Get())
                    {
                        manufacturer = obj["Manufacturer"]?.ToString() ?? manufacturer;
                        model = obj["Product"]?.ToString() ?? model;
                        break;
                    }
                }
                catch { }

                return new MotherboardInfo(manufacturer, model);
            });
        }

        public Task<List<NetworkAdapterInfo>> GetNetworkAdaptersAsync()
        {
            return Task.Run(() =>
            {
                var adapters = new List<NetworkAdapterInfo>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapterConfiguration where IPEnabled = 'True'");
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Description"]?.ToString() ?? "Unknown";
                        string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
                        string[] ips = (string[])obj["IPAddress"];
                        string ip = ips != null && ips.Length > 0 ? ips[0] : "N/A";
                        
                        adapters.Add(new NetworkAdapterInfo(name, mac, ip, "Detecting..."));
                    }
                }
                catch { }
                return adapters;
            });
        }
    }
}

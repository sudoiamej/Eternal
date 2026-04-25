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
        private readonly EnumerationOptions _wmiOptions;

        public WindowsHardwareService(ILibreHardwareService libreService)
        {
            _libreService = libreService;
            _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };
        }

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public Task<CpuInfo> GetCpuInfoAsync()
        {
            return Task.Run(() =>
            {
                string name = "Unknown CPU";
                int cores = Environment.ProcessorCount / 2; // Rough estimate
                int threads = Environment.ProcessorCount;
                string arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                string freq = "N/A";

                try
                {
                    // 1. Try WMI first
                    using var searcher = CreateSearcher("select Name, NumberOfCores, NumberOfLogicalProcessors, Architecture, MaxClockSpeed from Win32_Processor");
                    foreach (var obj in searcher.Get())
                    {
                        name = obj["Name"]?.ToString() ?? name;
                        cores = global::System.Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                        threads = global::System.Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                        arch = GetArchitecture(global::System.Convert.ToInt32(obj["Architecture"] ?? 0));
                        freq = $"{obj["MaxClockSpeed"]} MHz";
                        return new CpuInfo(name, cores, threads, arch, freq);
                    }
                }
                catch { }

                // 2. Fallback to Registry (Almost always works in PE)
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                    {
                        if (key != null)
                        {
                            name = key.GetValue("ProcessorNameString")?.ToString() ?? name;
                            freq = $"{key.GetValue("~MHz")} MHz";
                        }
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
                    using var searcher = CreateSearcher("select Name, DriverVersion, AdapterRAM from Win32_VideoController");
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
                    using var searcher = CreateSearcher("select Capacity, Speed from Win32_PhysicalMemory");
                    foreach (var obj in searcher.Get())
                    {
                        totalBytes += global::System.Convert.ToInt64(obj["Capacity"] ?? 0);
                        slots++;
                        speed = $"{obj["Speed"]} MHz";
                    }
                }
                catch { }

                // Fallback for RAM size
                if (totalBytes == 0)
                {
                    // Basic fallback using GC or Environment
                    totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                }

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
                    using var searcher = CreateSearcher("select Model, Size, Status, InterfaceType from Win32_DiskDrive");
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

                // Critical WinPE Fallback: If no physical disks found via WMI, list partitions
                if (disks.Count == 0)
                {
                    foreach (var drive in global::System.IO.DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            disks.Add(new DiskInfo(
                                $"{drive.Name} [{drive.VolumeLabel}]", 
                                $"{drive.TotalSize / (1024 * 1024 * 1024)} GB", 
                                "Ready", 
                                drive.DriveType.ToString()));
                        }
                    }
                }

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
                    // 1. BaseBoard
                    using (var searcher = CreateSearcher("select Manufacturer, Product from Win32_BaseBoard"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            string? m = obj["Manufacturer"]?.ToString();
                            string? p = obj["Product"]?.ToString();

                            if (!IsGeneric(m)) manufacturer = m!;
                            if (!IsGeneric(p)) model = p!;
                            
                            if (!IsGeneric(manufacturer) && !IsGeneric(model)) break;
                        }
                    }

                    // 2. System Product Fallback
                    if (IsGeneric(manufacturer) || IsGeneric(model))
                    {
                        using (var searcher = CreateSearcher("select Manufacturer, Name from Win32_ComputerSystemProduct"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                string? m = obj["Manufacturer"]?.ToString();
                                string? n = obj["Name"]?.ToString();

                                if (IsGeneric(manufacturer) && !IsGeneric(m)) manufacturer = m!;
                                if (IsGeneric(model) && !IsGeneric(n)) model = n!;
                                break;
                            }
                        }
                    }

                    // 3. Computer System Fallback
                    if (IsGeneric(manufacturer))
                    {
                        using (var searcher = CreateSearcher("select Manufacturer, Model from Win32_ComputerSystem"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                string? m = obj["Manufacturer"]?.ToString();
                                string? mod = obj["Model"]?.ToString();

                                if (IsGeneric(manufacturer) && !IsGeneric(m)) manufacturer = m!;
                                if (IsGeneric(model) && !IsGeneric(mod)) model = mod!;
                                break;
                            }
                        }
                    }
                }
                catch { }

                // Fallback to Registry for System Info
                try
                {
                    if (IsGeneric(manufacturer))
                        manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", manufacturer)?.ToString() ?? manufacturer;
                    
                    if (IsGeneric(model))
                        model = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", model)?.ToString() ?? model;
                }
                catch { }

                // Final cleaning
                if (IsGeneric(manufacturer)) manufacturer = "Standard PC";
                if (IsGeneric(model)) model = "Generic Board";

                return new MotherboardInfo(manufacturer, model);
            });
        }

        private bool IsGeneric(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string v = value.Trim().ToUpper();
            return v == "UNKNOWN" || 
                   v == "TO BE FILLED BY O.E.M." || 
                   v == "SYSTEM MANUFACTURER" || 
                   v == "SYSTEM PRODUCT NAME" || 
                   v == "DEFAULT STRING" ||
                   v == "O.E.M." ||
                   v == "NOT AVAILABLE" ||
                   v == "NOT SPECIFIED" ||
                   v == "NONE" ||
                   v == "INVALID" ||
                   v.Contains("GENERIC");
        }

        public Task<List<NetworkAdapterInfo>> GetNetworkAdaptersAsync()
        {
            return Task.Run(() =>
            {
                var adapters = new List<NetworkAdapterInfo>();
                try
                {
                    using var searcher = CreateSearcher("select Description, MACAddress, IPAddress from Win32_NetworkAdapterConfiguration where IPEnabled = 'True'");
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

        public Task<List<SystemSummaryItem>> GetDetailedSystemInfoAsync()
        {
            return Task.Run(() =>
            {
                var items = new List<SystemSummaryItem>();
                
                // 1. OS Info
                try {
                    using var searcher = CreateSearcher("select Caption, Version, BuildNumber, Manufacturer, CSName, OSArchitecture, BootDevice, WindowsDirectory, SystemDirectory from Win32_OperatingSystem");
                    foreach (var obj in searcher.Get()) {
                        items.Add(new SystemSummaryItem("OS", "OS Name", obj["Caption"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "Version", obj["Version"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "Build Number", obj["BuildNumber"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "OS Manufacturer", obj["Manufacturer"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "System Name", obj["CSName"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "Architecture", obj["OSArchitecture"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "Boot Device", obj["BootDevice"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "Windows Directory", obj["WindowsDirectory"]?.ToString()));
                        items.Add(new SystemSummaryItem("OS", "System Directory", obj["SystemDirectory"]?.ToString()));
                    }
                } catch { }

                // 2. System Info
                try {
                    using var searcher = CreateSearcher("select Manufacturer, Model, SystemType, SystemSKUNumber, TotalPhysicalMemory, Domain, UserName from Win32_ComputerSystem");
                    foreach (var obj in searcher.Get()) {
                        items.Add(new SystemSummaryItem("System", "System Manufacturer", obj["Manufacturer"]?.ToString()));
                        items.Add(new SystemSummaryItem("System", "System Model", obj["Model"]?.ToString()));
                        items.Add(new SystemSummaryItem("System", "System Type", obj["SystemType"]?.ToString()));
                        items.Add(new SystemSummaryItem("System", "SKU Number", obj["SystemSKUNumber"]?.ToString()));
                        
                        ulong totalBytes = global::System.Convert.ToUInt64(obj["TotalPhysicalMemory"] ?? 0);
                        string memoryStr = (totalBytes / (1024 * 1024 * 1024)).ToString() + " GB";
                        items.Add(new SystemSummaryItem("System", "Total Physical Memory", memoryStr));
                        
                        items.Add(new SystemSummaryItem("System", "Domain", obj["Domain"]?.ToString()));
                        items.Add(new SystemSummaryItem("System", "User Name", obj["UserName"]?.ToString()));
                    }
                } catch { }

                // 3. BIOS Info
                try {
                    using var searcher = CreateSearcher("select SMBIOSBIOSVersion, SMBIOSMajorVersion, SMBIOSMinorVersion, EmbeddedControllerMajorVersion, EmbeddedControllerMinorVersion from Win32_BIOS");
                    foreach (var obj in searcher.Get()) {
                        items.Add(new SystemSummaryItem("Firmware", "BIOS Version/Date", obj["SMBIOSBIOSVersion"]?.ToString()));
                        
                        string smbios = (obj["SMBIOSMajorVersion"]?.ToString() ?? "0") + "." + (obj["SMBIOSMinorVersion"]?.ToString() ?? "0");
                        items.Add(new SystemSummaryItem("Firmware", "SMBIOS Version", smbios));
                        
                        string ec = (obj["EmbeddedControllerMajorVersion"]?.ToString() ?? "0") + "." + (obj["EmbeddedControllerMinorVersion"]?.ToString() ?? "0");
                        items.Add(new SystemSummaryItem("Firmware", "Embedded Controller Version", ec));
                        
                        items.Add(new SystemSummaryItem("Firmware", "BIOS Mode", "N/A (See UEFI Module)"));
                    }
                } catch { }

                // 4. BaseBoard
                try {
                    using var searcher = CreateSearcher("select Manufacturer, Product, Version from Win32_BaseBoard");
                    foreach (var obj in searcher.Get()) {
                        items.Add(new SystemSummaryItem("Board", "BaseBoard Manufacturer", obj["Manufacturer"]?.ToString()));
                        items.Add(new SystemSummaryItem("Board", "BaseBoard Product", obj["Product"]?.ToString()));
                        items.Add(new SystemSummaryItem("Board", "BaseBoard Version", obj["Version"]?.ToString()));
                    }
                } catch { }

                // 5. Processor (Detailed)
                try {
                    using var searcher = CreateSearcher("select Name, Description, L2CacheSize, L3CacheSize from Win32_Processor");
                    foreach (var obj in searcher.Get()) {
                        items.Add(new SystemSummaryItem("Processor", "Name", obj["Name"]?.ToString()));
                        items.Add(new SystemSummaryItem("Processor", "Description", obj["Description"]?.ToString()));
                        
                        string l2 = (obj["L2CacheSize"]?.ToString() ?? "0") + " KB";
                        string l3 = (obj["L3CacheSize"]?.ToString() ?? "0") + " KB";
                        items.Add(new SystemSummaryItem("Processor", "L2 Cache Size", l2));
                        items.Add(new SystemSummaryItem("Processor", "L3 Cache Size", l3));
                    }
                } catch { }

                return items;
            });
        }
        private List<CancellationTokenSource> _stressCancelTokens = new List<CancellationTokenSource>();

        public void StartStressTest(int threads)
        {
            StopStressTest();
            for (int i = 0; i < threads; i++)
            {
                var cts = new CancellationTokenSource();
                _stressCancelTokens.Add(cts);
                Task.Run(() => RunHeavyPrimes(cts.Token), cts.Token);
            }
        }

        public void StopStressTest()
        {
            foreach (var cts in _stressCancelTokens)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _stressCancelTokens.Clear();
        }

        private void RunHeavyPrimes(CancellationToken token)
        {
            long number = 1000000;
            while (!token.IsCancellationRequested)
            {
                IsPrime(number++);
            }
        }

        private bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            var boundary = (long)Math.Floor(Math.Sqrt(number));
            for (long i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }
    }
}

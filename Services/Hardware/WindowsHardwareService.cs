using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Eternal.Services.System;
using Eternal.Models;
using Eternal.Helpers;

namespace Eternal.Services.Hardware
{
    public class WindowsHardwareService : IHardwareService
    {
        private readonly ILibreHardwareService _libreService;
        private readonly EnumerationOptions _wmiOptions;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public void Init() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

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
                int cores = Environment.ProcessorCount / 2; // Default heuristic
                int threads = Environment.ProcessorCount;
                string arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                string freq = "N/A";

                bool useNative = OsHelper.IsWindows11OrGreater();

                // 1. Primary for Windows 11: Native Registry (Fast, non-deprecated)
                if (useNative)
                {
                    try
                    {
                        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                        if (key != null)
                        {
                            name = key.GetValue("ProcessorNameString")?.ToString() ?? name;
                            var mhz = key.GetValue("~MHz");
                            if (mhz != null) freq = $"{mhz} MHz";
                        }
                    }
                    catch { }
                }

                // 2. Secondary/Fallback: WMI (Robust details)
                if (name == "Unknown CPU" || !useNative)
                {
                    try
                    {
                        using var searcher = CreateSearcher("select Name, NumberOfCores, NumberOfLogicalProcessors, Architecture, MaxClockSpeed from Win32_Processor");
                        using var collection = searcher.Get();
                        foreach (ManagementObject obj in collection)
                        {
                            using (obj)
                            {
                                name = obj["Name"]?.ToString() ?? name;
                                cores = global::System.Convert.ToInt32(obj["NumberOfCores"] ?? cores);
                                threads = global::System.Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? threads);
                                arch = GetArchitecture(global::System.Convert.ToInt32(obj["Architecture"] ?? 0));
                                freq = $"{obj["MaxClockSpeed"]} MHz";
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"WMI CPU Scan Error: {ex.Message}");
                    }
                }

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
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            name = obj["Name"]?.ToString() ?? name;
                            driver = obj["DriverVersion"]?.ToString() ?? driver;
                            long bytes = global::System.Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                            vram = $"{bytes / (1024 * 1024)} MB";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"WMI GPU Scan Error: {ex.Message}");
                }

                // Try querying Task Manager / WDDM GPU counters first for temperature
                try
                {
                    using var perfSearcher = new ManagementObjectSearcher(@"root\cimv2", "select Temperature from Win32_PerfFormattedData_GPUPerformanceCounters_GPUDevice");
                    foreach (ManagementObject obj in perfSearcher.Get())
                    {
                        double pTemp = Convert.ToDouble(obj["Temperature"]);
                        if (pTemp > 0 && pTemp < 120)
                        {
                            temp = $"{pTemp:F0}°C";
                            break;
                        }
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
                                else if (sensor.SensorType == SensorType.Temperature)
                                {
                                    if (sensor.Name.Contains("GPU Core") || sensor.Name.Contains("Core") || sensor.Name.Contains("Temp") || temp == "N/A")
                                        temp = $"{sensor.Value:F0}°C";
                                }
                                else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
                                    coreClock = $"{sensor.Value:F0} MHz";
                                else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Memory"))
                                    memClock = $"{sensor.Value:F0} MHz";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"Libre GPU Scan Error: {ex.Message}");
                }

                return new GpuInfo(name, driver, vram, util, temp, coreClock, memClock, cores);
            });
        }

        public Task<RamInfo> GetRamInfoAsync()
        {
            return Task.Run(() =>
            {
                long totalBytes = 0;
                long availBytes = 0;
                int slots = 0;
                string speed = "Unknown";

                bool useNative = OsHelper.IsWindows11OrGreater();

                // 1. Current Usage & Total via P/Invoke (Extremely reliable)
                try
                {
                    var memStatus = new MEMORYSTATUSEX();
                    memStatus.Init();
                    if (GlobalMemoryStatusEx(ref memStatus))
                    {
                        totalBytes = (long)memStatus.ullTotalPhys;
                        availBytes = (long)memStatus.ullAvailPhys;
                    }
                }
                catch { }

                // 2. Supplemental details (Slots, Speed) via WMI
                try
                {
                    using var searcher = CreateSearcher("select Capacity, Speed from Win32_PhysicalMemory");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            if (totalBytes == 0) totalBytes += global::System.Convert.ToInt64(obj["Capacity"] ?? 0);
                            slots++;
                            speed = $"{obj["Speed"]} MHz";
                        }
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"WMI RAM Scan Error: {ex.Message}");
                }

                if (totalBytes == 0)
                {
                    totalBytes = (long)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                }

                long usedBytes = totalBytes - availBytes;
                string usedStr = $"{usedBytes / (1024 * 1024 * 1024.0):F1} GB";

                return new RamInfo($"{totalBytes / (1024 * 1024 * 1024)} GB", usedStr, speed, slots, slots);
            });
        }

        public Task<List<DiskInfo>> GetDiskInfoAsync()
        {
            return Task.Run(() =>
            {
                var disks = new List<DiskInfo>();
                try
                {
                    using var storageSearcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT FriendlyName, Size, HealthStatus, MediaType FROM MSFT_PhysicalDisk");
                    using var collection = storageSearcher.Get();
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            string model = obj["FriendlyName"]?.ToString() ?? "Unknown";
                            long bytes = global::System.Convert.ToInt64(obj["Size"] ?? 0);
                            string size = $"{bytes / (1024 * 1024 * 1024)} GB";
                            
                            int healthStatus = global::System.Convert.ToInt32(obj["HealthStatus"] ?? 0);
                            string health = healthStatus switch
                            {
                                0 => "Healthy",
                                1 => "Warning",
                                2 => "Unhealthy",
                                _ => "Unknown"
                            };

                            int mediaVal = global::System.Convert.ToInt32(obj["MediaType"] ?? 0);
                            string interfaceType = mediaVal switch
                            {
                                3 => "HDD",
                                4 => "SSD",
                                5 => "SCM",
                                _ => "Storage"
                            };

                            disks.Add(new DiskInfo(model, size, health, interfaceType));
                        }
                    }
                }
                catch
                {
                    // Fallback to legacy Win32_DiskDrive
                    try
                    {
                        using var searcher = CreateSearcher("select Model, Size, Status, InterfaceType from Win32_DiskDrive");
                        using var collection = searcher.Get();
                        foreach (ManagementObject obj in collection)
                        {
                            using (obj)
                            {
                                string model = obj["Model"]?.ToString() ?? "Unknown";
                                long bytes = global::System.Convert.ToInt64(obj["Size"] ?? 0);
                                string size = $"{bytes / (1024 * 1024 * 1024)} GB";
                                string health = obj["Status"]?.ToString() ?? "Unknown";
                                string interfaceType = obj["InterfaceType"]?.ToString() ?? "Unknown";
                                disks.Add(new DiskInfo(model, size, health, interfaceType));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"WMI Disk Scan Error: {ex.Message}");
                    }
                }

                // Critical WinPE Fallback: If no physical disks found via WMI, list partitions
                if (disks.Count == 0)
                {
                    try
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
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"Fallback Disk Scan Error: {ex.Message}");
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

                bool useNative = OsHelper.IsWindows11OrGreater();

                // 1. Primary for Win11: Registry (SMBIOS Strings)
                if (useNative)
                {
                    try
                    {
                        manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer", manufacturer)?.ToString() ?? manufacturer;
                        model = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", model)?.ToString() ?? model;

                        if (IsGeneric(manufacturer))
                            manufacturer = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", manufacturer)?.ToString() ?? manufacturer;
                        
                        if (IsGeneric(model))
                            model = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", model)?.ToString() ?? model;
                    }
                    catch { }
                }

                // 2. Secondary/Fallback: WMI
                if (IsGeneric(manufacturer) || IsGeneric(model) || !useNative)
                {
                    try
                    {
                        using (var searcher = CreateSearcher("select Manufacturer, Product from Win32_BaseBoard"))
                        {
                            using var collection = searcher.Get();
                            foreach (ManagementObject obj in collection)
                            {
                                using (obj)
                                {
                                    string? m = obj["Manufacturer"]?.ToString();
                                    string? p = obj["Product"]?.ToString();
                                    if (!IsGeneric(m)) manufacturer = m!;
                                    if (!IsGeneric(p)) model = p!;
                                    if (!IsGeneric(manufacturer) && !IsGeneric(model)) break;
                                }
                            }
                        }
                    }
                    catch { }
                }

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
                    // Map Configuration to Adapter for Speed
                    var speeds = new Dictionary<string, string>();
                    using (var speedSearcher = CreateSearcher("select Name, Speed from Win32_NetworkAdapter"))
                    {
                        using var speedCol = speedSearcher.Get();
                        foreach (ManagementObject sObj in speedCol)
                        {
                            using (sObj)
                            {
                                string? n = sObj["Name"]?.ToString();
                                string? s = sObj["Speed"]?.ToString();
                                if (n != null && s != null)
                                {
                                    long bitSpeed = global::System.Convert.ToInt64(s);
                                    speeds[n] = bitSpeed >= 1000000000 ? $"{bitSpeed / 1000000000.0:F1} Gbps" : $"{bitSpeed / 1000000.0:F0} Mbps";
                                }
                            }
                        }
                    }

                    using var searcher = CreateSearcher("select Description, MACAddress, IPAddress from Win32_NetworkAdapterConfiguration where IPEnabled = 'True'");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            string name = obj["Description"]?.ToString() ?? "Unknown";
                            string mac = obj["MACAddress"]?.ToString() ?? "Unknown";
                            string[] ips = (string[])obj["IPAddress"];
                            string ip = ips != null && ips.Length > 0 ? ips[0] : "N/A";
                            
                            speeds.TryGetValue(name, out var speed);
                            adapters.Add(new NetworkAdapterInfo(name, mac, ip, speed ?? "Unknown"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"WMI Network Scan Error: {ex.Message}");
                }
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
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection) {
                        using (obj)
                        {
                            items.Add(new SystemSummaryItem("OS", "OS Name", obj["Caption"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "Version", obj["Version"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "Build Number", obj["BuildNumber"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "OS Manufacturer", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "System Name", obj["CSName"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "Architecture", obj["OSArchitecture"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "Boot Device", obj["BootDevice"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "Windows Directory", obj["WindowsDirectory"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("OS", "System Directory", obj["SystemDirectory"]?.ToString() ?? "Unknown"));
                        }
                    }
                } catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"WMI OS Scan Error: {ex.Message}"); }

                // 2. System Info
                try {
                    using var searcher = CreateSearcher("select Manufacturer, Model, SystemType, SystemSKUNumber, TotalPhysicalMemory, Domain, UserName from Win32_ComputerSystem");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection) {
                        using (obj)
                        {
                            items.Add(new SystemSummaryItem("System", "System Manufacturer", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("System", "System Model", obj["Model"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("System", "System Type", obj["SystemType"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("System", "SKU Number", obj["SystemSKUNumber"]?.ToString() ?? "Unknown"));
                            
                            ulong totalBytes = global::System.Convert.ToUInt64(obj["TotalPhysicalMemory"] ?? 0);
                            string memoryStr = (totalBytes / (1024 * 1024 * 1024)).ToString() + " GB";
                            items.Add(new SystemSummaryItem("System", "Total Physical Memory", memoryStr));
                            
                            items.Add(new SystemSummaryItem("System", "Domain", obj["Domain"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("System", "User Name", obj["UserName"]?.ToString() ?? "Unknown"));
                        }
                    }
                } catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"WMI ComputerSystem Scan Error: {ex.Message}"); }

                // 3. BIOS Info
                try {
                    using var searcher = CreateSearcher("select SMBIOSBIOSVersion, SMBIOSMajorVersion, SMBIOSMinorVersion, EmbeddedControllerMajorVersion, EmbeddedControllerMinorVersion from Win32_BIOS");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection) {
                        using (obj)
                        {
                            items.Add(new SystemSummaryItem("Firmware", "BIOS Version/Date", obj["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown"));
                            
                            string smbios = (obj["SMBIOSMajorVersion"]?.ToString() ?? "0") + "." + (obj["SMBIOSMinorVersion"]?.ToString() ?? "0");
                            items.Add(new SystemSummaryItem("Firmware", "SMBIOS Version", smbios));
                            
                            string ec = (obj["EmbeddedControllerMajorVersion"]?.ToString() ?? "0") + "." + (obj["EmbeddedControllerMinorVersion"]?.ToString() ?? "0");
                            items.Add(new SystemSummaryItem("Firmware", "Embedded Controller Version", ec));
                            
                            items.Add(new SystemSummaryItem("Firmware", "BIOS Mode", "N/A (See UEFI Module)"));
                        }
                    }
                } catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"WMI BIOS Scan Error: {ex.Message}"); }

                // 4. BaseBoard
                try {
                    using var searcher = CreateSearcher("select Manufacturer, Product, Version from Win32_BaseBoard");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection) {
                        using (obj)
                        {
                            items.Add(new SystemSummaryItem("Board", "BaseBoard Manufacturer", obj["Manufacturer"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("Board", "BaseBoard Product", obj["Product"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("Board", "BaseBoard Version", obj["Version"]?.ToString() ?? "Unknown"));
                        }
                    }
                } catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"WMI BaseBoard Scan Error: {ex.Message}"); }

                // 5. Processor (Detailed)
                try {
                    using var searcher = CreateSearcher("select Name, Description, L2CacheSize, L3CacheSize from Win32_Processor");
                    using var collection = searcher.Get();
                    foreach (ManagementObject obj in collection) {
                        using (obj)
                        {
                            items.Add(new SystemSummaryItem("Processor", "Name", obj["Name"]?.ToString() ?? "Unknown"));
                            items.Add(new SystemSummaryItem("Processor", "Description", obj["Description"]?.ToString() ?? "Unknown"));
                            
                            string l2 = (obj["L2CacheSize"]?.ToString() ?? "0") + " KB";
                            string l3 = (obj["L3CacheSize"]?.ToString() ?? "0") + " KB";
                            items.Add(new SystemSummaryItem("Processor", "L2 Cache Size", l2));
                            items.Add(new SystemSummaryItem("Processor", "L3 Cache Size", l3));
                        }
                    }
                } catch (Exception ex) { global::System.Diagnostics.Debug.WriteLine($"WMI Processor Detail Scan Error: {ex.Message}"); }

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

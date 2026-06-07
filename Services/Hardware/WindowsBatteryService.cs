using System;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.Helpers;
using LibreHardwareMonitor.Hardware;

namespace Eternal.Services.Hardware
{
    public class WindowsBatteryService : IBatteryService
    {
        private readonly ILibreHardwareService _libreService;

        #region P/Invoke ACPI Battery Driver SetupAPI & DeviceIoControl

        private static readonly Guid GUID_DEVCLASS_BATTERY = new Guid("72631e54-78a4-11d0-bcf7-00aa00b7b32a");

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref uint lpInBuffer, uint nInBufferSize, ref uint lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref BATTERY_QUERY_INFORMATION lpInBuffer, uint nInBufferSize, ref BATTERY_INFORMATION lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref BATTERY_QUERY_INFORMATION lpInBuffer, uint nInBufferSize, ref BATTERY_STATUS lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        private const uint IOCTL_BATTERY_QUERY_TAG = 0x294040;
        private const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x294044;
        private const uint IOCTL_BATTERY_QUERY_STATUS = 0x29404c;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BATTERY_QUERY_INFORMATION
        {
            public uint BatteryTag;
            public int InformationLevel;
            public int AtRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BATTERY_INFORMATION
        {
            public uint Capabilities;
            public byte Technology;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Chemistry;
            public uint DesignedCapacity;
            public uint FullChargedCapacity;
            public uint DefaultAlert1;
            public uint DefaultAlert2;
            public uint CriticalBias;
            public uint CycleCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BATTERY_STATUS
        {
            public uint PowerState;
            public uint Capacity;
            public uint Voltage;
            public int Rate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        #endregion

        public WindowsBatteryService(ILibreHardwareService libreService)
        {
            _libreService = libreService;
        }

        private BatteryInfo? GetBatteryInfoFromDriver()
        {
            var batteryGuid = GUID_DEVCLASS_BATTERY;
            IntPtr hDevInfo = SetupDiGetClassDevs(ref batteryGuid, null!, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (hDevInfo == IntPtr.Zero || hDevInfo.ToInt64() == -1) return null;

            try
            {
                SP_DEVICE_INTERFACE_DATA did = new SP_DEVICE_INTERFACE_DATA();
                did.cbSize = (uint)Marshal.SizeOf(did);

                if (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref batteryGuid, 0, ref did))
                {
                    uint requiredSize = 0;
                    SetupDiGetDeviceInterfaceDetail(hDevInfo, ref did, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

                    IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        Marshal.WriteInt32(detailDataBuffer, IntPtr.Size == 8 ? 8 : 6);

                        if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref did, detailDataBuffer, requiredSize, out _, IntPtr.Zero))
                        {
                            IntPtr pDevicePath = new IntPtr(detailDataBuffer.ToInt64() + 4);
                            string? devicePath = Marshal.PtrToStringAuto(pDevicePath);

                            if (!string.IsNullOrEmpty(devicePath))
                            {
                                IntPtr hBattery = CreateFile(devicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                                if (hBattery != IntPtr.Zero && hBattery.ToInt64() != -1)
                                {
                                    try
                                    {
                                        uint queryTagVal = 0;
                                        uint batteryTag = 0;
                                        if (DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_TAG, ref queryTagVal, sizeof(uint), ref batteryTag, sizeof(uint), out _, IntPtr.Zero) && batteryTag != 0)
                                        {
                                            var bqi = new BATTERY_QUERY_INFORMATION
                                            {
                                                BatteryTag = batteryTag,
                                                InformationLevel = 0,
                                                AtRate = 0
                                            };
                                            var bi = new BATTERY_INFORMATION();
                                            if (DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_INFORMATION, ref bqi, (uint)Marshal.SizeOf(bqi), ref bi, (uint)Marshal.SizeOf(bi), out _, IntPtr.Zero))
                                            {
                                                var bs = new BATTERY_STATUS();
                                                if (DeviceIoControl(hBattery, IOCTL_BATTERY_QUERY_STATUS, ref bqi, (uint)Marshal.SizeOf(bqi), ref bs, (uint)Marshal.SizeOf(bs), out _, IntPtr.Zero))
                                                {
                                                    bool acOnline = (bs.PowerState & 2) != 0;
                                                    bool discharging = (bs.PowerState & 1) != 0;

                                                    ChargingState cState = ChargingState.Unknown;
                                                    if (acOnline)
                                                    {
                                                        cState = bs.Capacity >= bi.FullChargedCapacity * 0.98 ? ChargingState.Full : ChargingState.Charging;
                                                    }
                                                    else if (discharging)
                                                    {
                                                        cState = ChargingState.Discharging;
                                                    }

                                                    double wear = 0;
                                                    if (bi.DesignedCapacity > 0 && bi.FullChargedCapacity > 0)
                                                    {
                                                        wear = Math.Max(0, 100 - ((double)bi.FullChargedCapacity / bi.DesignedCapacity * 100));
                                                    }

                                                    double wattage = Math.Abs(bs.Rate) / 1000.0;
                                                    double voltage = bs.Voltage / 1000.0;

                                                    int chargeLevel = bi.FullChargedCapacity > 0 ? (int)((double)bs.Capacity / bi.FullChargedCapacity * 100) : 0;
                                                    chargeLevel = Math.Clamp(chargeLevel, 0, 100);

                                                    string chemistry = "Li-ion";
                                                    if (bi.Chemistry != null)
                                                    {
                                                        chemistry = global::System.Text.Encoding.ASCII.GetString(bi.Chemistry).Trim('\0', ' ');
                                                    }

                                                    TimeSpan remaining = TimeSpan.Zero;
                                                    if (discharging && bs.Rate < 0 && bs.Capacity > 0)
                                                    {
                                                        double hours = (double)bs.Capacity / Math.Abs(bs.Rate);
                                                        remaining = TimeSpan.FromHours(hours);
                                                    }
                                                    else if (acOnline && bs.Rate > 0 && bs.Capacity < bi.FullChargedCapacity)
                                                    {
                                                        double hours = (double)(bi.FullChargedCapacity - bs.Capacity) / bs.Rate;
                                                        remaining = TimeSpan.FromHours(hours);
                                                    }

                                                    return new BatteryInfo(
                                                        acOnline ? "OK (AC)" : "OK (Battery)",
                                                        chargeLevel,
                                                        acOnline ? "AC Adapter" : "Battery",
                                                        wear,
                                                        (int)bi.DesignedCapacity,
                                                        (int)bi.FullChargedCapacity,
                                                        (int)bs.Capacity,
                                                        (int)bi.CycleCount,
                                                        chemistry,
                                                        "ACPI Battery Device",
                                                        28.5,
                                                        voltage,
                                                        wattage,
                                                        remaining,
                                                        cState
                                                    );
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        CloseHandle(hBattery);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailDataBuffer);
                    }
                }
            }
            catch { }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
            return null;
        }

        public async Task<BatteryInfo?> GetBatteryInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Core ACPI Driver Query (DeviceIoControl - Highest Accuracy)
                    var driverInfo = GetBatteryInfoFromDriver();
                    if (driverInfo != null)
                    {
                        double temp = driverInfo.Temperature;
                        double wattage = driverInfo.ChargeRateWattage;
                        try
                        {
                            _libreService.Update();
                            var battery = _libreService.Computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Battery);
                            if (battery != null)
                            {
                                foreach (var sensor in battery.Sensors)
                                {
                                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                        temp = sensor.Value.Value;
                                    else if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                                        wattage = sensor.Value.Value;
                                }
                            }
                        }
                        catch { }

                        return new BatteryInfo(
                            driverInfo.Status,
                            driverInfo.ChargeLevel,
                            driverInfo.PowerSource,
                            driverInfo.WearLevel,
                            driverInfo.DesignCapacity,
                            driverInfo.FullChargeCapacity,
                            driverInfo.CurrentCapacity,
                            driverInfo.CycleCount,
                            driverInfo.Chemistry,
                            driverInfo.DeviceName,
                            temp,
                            driverInfo.Voltage,
                            wattage,
                            driverInfo.EstimatedTimeRemaining,
                            driverInfo.ChargingState
                        );
                    }

                    // 2. Fallback to native Win32_Battery/GetSystemPowerStatus
                    bool useNative = OsHelper.IsWindows11OrGreater();
                    byte nativeCharge = 0;
                    string nativeSource = "Unknown";
                    ChargingState nativeState = ChargingState.Unknown;

                    if (useNative)
                    {
                        if (GetSystemPowerStatus(out var sps))
                        {
                            nativeCharge = sps.BatteryLifePercent == 255 ? (byte)0 : sps.BatteryLifePercent;
                            nativeSource = sps.ACLineStatus == 1 ? "AC Adapter" : "Battery";
                            nativeState = sps.ACLineStatus == 1 ? (nativeCharge >= 99 ? ChargingState.Full : ChargingState.Charging) : ChargingState.Discharging;
                        }
                    }

                    int cycleCount = 0;
                    int designCap = 0;
                    int fullCap = 0;
                    int curCap = 0;

                    try
                    {
                        using var wmiSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryCycleCount");
                        foreach (var obj in wmiSearcher.Get())
                        {
                            cycleCount = Convert.ToInt32(obj["CycleCount"] ?? 0);
                        }

                        using var fullCapSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryFullChargedCapacity");
                        foreach (var obj in fullCapSearcher.Get())
                        {
                            fullCap = Convert.ToInt32(obj["FullChargedCapacity"] ?? 0);
                        }

                        using var staticSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryStaticData");
                        foreach (var obj in staticSearcher.Get())
                        {
                            designCap = Convert.ToInt32(obj["DesignedCapacity"] ?? 0);
                        }

                        using var statusSearcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryStatus");
                        foreach (var obj in statusSearcher.Get())
                        {
                            curCap = Convert.ToInt32(obj["RemainingCapacity"] ?? 0);
                        }
                    }
                    catch { }

                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                    bool foundWmi = false;
                    foreach (var obj in searcher.Get())
                    {
                        foundWmi = true;
                        string status = obj["Status"]?.ToString() ?? "Unknown";
                        int charge = useNative ? nativeCharge : Convert.ToInt32(obj["EstimatedChargeRemaining"] ?? 0);
                        
                        if (designCap == 0) designCap = Convert.ToInt32(obj["DesignCapacity"] ?? 0);
                        if (fullCap == 0) fullCap = Convert.ToInt32(obj["FullChargeCapacity"] ?? 0);
                        if (curCap == 0) curCap = (int)(fullCap * (charge / 100.0));
                        
                        double wear = 0;
                        if (designCap > 0 && fullCap > 0)
                        {
                            wear = Math.Max(0, 100 - ((double)fullCap / designCap * 100));
                        }

                        string name = obj["DeviceID"]?.ToString() ?? obj["Name"]?.ToString() ?? "Standard Battery";
                        string chemistry = obj["Chemistry"]?.ToString() ?? "Li-ion";
                        
                        int statusId = Convert.ToInt32(obj["BatteryStatus"] ?? 0);
                        string pwrSource = useNative ? nativeSource : (statusId == 2 || statusId == 6 || statusId == 7 ? "AC Adapter" : "Battery");
                        
                        ChargingState chargingState = useNative ? nativeState : statusId switch {
                            1 => ChargingState.Discharging,
                            2 => ChargingState.Bypass,
                            6 => ChargingState.Charging,
                            7 => ChargingState.Charging,
                            3 => ChargingState.Full,
                            _ => ChargingState.Unknown
                        };

                        double voltage = Convert.ToDouble(obj["DesignVoltage"] ?? 11100) / 1000.0;
                        double temp = 28.5;
                        double wattage = chargingState == ChargingState.Charging ? 45.0 : (chargingState == ChargingState.Discharging ? 15.0 : 0.0);

                        try
                        {
                            _libreService.Update();
                            var battery = _libreService.Computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Battery);
                            if (battery != null)
                            {
                                foreach (var sensor in battery.Sensors)
                                {
                                    if (sensor.SensorType == SensorType.Voltage && sensor.Value.HasValue)
                                        voltage = sensor.Value.Value;
                                    else if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                        temp = sensor.Value.Value;
                                    else if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                                        wattage = sensor.Value.Value;
                                }
                            }
                        }
                        catch { }
                        
                        int timeMins = Convert.ToInt32(obj["EstimatedRunTime"] ?? 0);
                        TimeSpan remaining = timeMins > 0 ? TimeSpan.FromMinutes(timeMins) : TimeSpan.Zero;

                        return new BatteryInfo(
                            status, charge, pwrSource, wear, designCap, fullCap, curCap, cycleCount, chemistry, name,
                            temp, voltage, wattage, remaining, chargingState
                        );
                    }

                    if (!foundWmi && useNative)
                    {
                        return new BatteryInfo(
                            "OK", nativeCharge, nativeSource, 0, 0, 0, 0, 0, "Unknown", "System Battery",
                            28.5, 11.1, 0, TimeSpan.Zero, nativeState
                        );
                    }
                }
                catch { }
                return null;
            });
        }
    }
}

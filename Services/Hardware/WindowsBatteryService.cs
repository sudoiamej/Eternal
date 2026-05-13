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

        public WindowsBatteryService(ILibreHardwareService libreService)
        {
            _libreService = libreService;
        }

        public async Task<BatteryInfo?> GetBatteryInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    bool useNative = OsHelper.IsWindows11OrGreater();
                    
                    // Native State (P/Invoke) - High reliability for basic status
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

                    // 1. Primary Hardware Data from root\WMI (Advanced hardware-level data)
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
                    catch { /* WMI root\WMI might be restricted or unsupported on some hardware */ }

                    // 2. Fallback/Supplemental Data from Win32_Battery
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                    bool foundWmi = false;
                    foreach (var obj in searcher.Get())
                    {
                        foundWmi = true;
                        string status = obj["Status"]?.ToString() ?? "Unknown";
                        int charge = useNative ? nativeCharge : Convert.ToInt32(obj["EstimatedChargeRemaining"] ?? 0);
                        
                        // Use Win32 values if WMI root didn't provide them
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

                    // 3. Ultra-Fallback (Registry/Native only) - For when WMI is completely absent
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

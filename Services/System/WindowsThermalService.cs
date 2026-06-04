using System;
using System.Management;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace Eternal.Services.System
{
    public interface IThermalService
    {
        Task<ThermalSnapshot> GetThermalDataAsync();
    }

    public record ThermalSnapshot(
        double CpuTemp, 
        double GpuTemp, 
        double CpuPower, 
        double CpuVoltage,
        double FanSpeed,
        string PowerSource, 
        int BatteryPercent, 
        string BatteryStatus, 
        bool HasBattery,
        List<string> OtherSensors);

    public class WindowsThermalService : IThermalService, IDisposable
    {
        private readonly ILibreHardwareService _libreService;
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public WindowsThermalService(ILibreHardwareService libreService)
        {
            _libreService = libreService;
        }

        public Task<ThermalSnapshot> GetThermalDataAsync()
        {
            return Task.Run(() =>
            {
                double cpuTemp = -1;
                double gpuTemp = -1;
                double cpuPower = 0;
                double cpuVoltage = 0;
                double fanSpeed = 0;
                var otherSensors = new List<string>();
                
                // 1. Try WDDM Performance Counters first for GPU Temperature (same as Task Manager)
                gpuTemp = GetGpuTempFromPerfCounters();

                try
                {
                    _libreService.Update();
                    foreach (IHardware hardware in _libreService.Computer.Hardware)
                    {
                        // CPU Telemetry
                        if (hardware.HardwareType == HardwareType.Cpu)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    if (sensor.Name.Contains("Core Max") || sensor.Name.Contains("Package"))
                                        cpuTemp = sensor.Value ?? cpuTemp;
                                    else if (cpuTemp == -1) 
                                        cpuTemp = sensor.Value ?? cpuTemp;
                                }
                                else if (sensor.SensorType == SensorType.Power && (sensor.Name.Contains("Package") || sensor.Name.Contains("Total")))
                                {
                                    cpuPower = sensor.Value ?? cpuPower;
                                }
                                else if (sensor.SensorType == SensorType.Voltage && sensor.Name.Contains("Core"))
                                {
                                    cpuVoltage = sensor.Value ?? cpuVoltage;
                                }
                            }
                        }
                        // GPU Telemetry (NVIDIA / AMD / Intel)
                        else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    // Update GPU Temp if not already retrieved via WDDM
                                    if (gpuTemp == -1)
                                    {
                                        if (sensor.Name.Contains("Core") || sensor.Name.Contains("Temp") || gpuTemp == -1)
                                            gpuTemp = sensor.Value ?? gpuTemp;
                                    }
                                    else
                                    {
                                        // Still capture secondary sensors (e.g. Memory, Hot Spot) in the auxiliary list
                                        otherSensors.Add($"{hardware.Name} {sensor.Name}: {sensor.Value:F1}°C");
                                    }
                                }
                                else if (sensor.SensorType == SensorType.Fan)
                                {
                                    fanSpeed = sensor.Value ?? fanSpeed;
                                }
                            }
                        }
                        // Motherboard / ACPI / SuperIO Chipset Sensors
                        else if (hardware.HardwareType == HardwareType.Motherboard)
                        {
                            foreach (var subHardware in hardware.SubHardware)
                            {
                                subHardware.Update();
                                foreach (var sensor in subHardware.Sensors)
                                {
                                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                                    {
                                        otherSensors.Add($"{subHardware.Name} {sensor.Name}: {sensor.Value.Value:F1}°C");
                                    }
                                    else if (sensor.SensorType == SensorType.Fan && fanSpeed == 0)
                                    {
                                        fanSpeed = sensor.Value ?? fanSpeed;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // 2. Fallbacks for CPU Temp (via WMI ACPI Thermal Zone)
                if (cpuTemp == -1)
                {
                    cpuTemp = GetWmiThermalZoneTemp();
                }

                // 3. Fallbacks for GPU Temp (if both WDDM and Libre failed)
                if (gpuTemp == -1)
                {
                    gpuTemp = GetFallbackGpuTemp();
                }

                // 4. Query ACPI Thermal Zones for auxiliary sensors
                QueryWmiThermalZones(otherSensors);

                string power = "Detecting...";
                int bat = 0;
                string status = "Unknown";
                bool hasBattery = false;

                try
                {
                    using var searcher = CreateSearcher("select EstimatedChargeRemaining, BatteryStatus from Win32_Battery");
                    foreach (var obj in searcher.Get())
                    {
                        hasBattery = true;
                        bat = global::System.Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                        int code = global::System.Convert.ToInt32(obj["BatteryStatus"]);
                        status = code == 2 ? "Charging" : "Discharging";
                        power = (code == 2 || code == 6 || code == 7) ? "Plugged In" : "On Battery";
                        break;
                    }
                }
                catch { }

                if (!hasBattery)
                {
                    power = "AC Power (Direct)";
                    status = "No Battery Detected";
                }

                return new ThermalSnapshot(cpuTemp, gpuTemp, cpuPower, cpuVoltage, fanSpeed, power, bat, status, hasBattery, otherSensors);
            });
        }

        private double GetGpuTempFromPerfCounters()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\cimv2", "select Temperature from Win32_PerfFormattedData_GPUPerformanceCounters_GPUDevice");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double temp = Convert.ToDouble(obj["Temperature"]);
                    if (temp > 0 && temp < 120)
                    {
                        return temp;
                    }
                }
            }
            catch { }
            return -1;
        }

        private double GetWmiThermalZoneTemp()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\wmi", "select CurrentTemperature from MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double kelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                    double celsius = (kelvin / 10.0) - 273.15;
                    if (celsius > 0 && celsius < 120)
                    {
                        return celsius;
                    }
                }
            }
            catch { }
            return -1;
        }

        private double GetFallbackGpuTemp()
        {
            // Fallback: Check Win32_VideoController temperature sensors (if any vendor exposes it there)
            try
            {
                using var searcher = new ManagementObjectSearcher("select CurrentTemperature from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double temp = Convert.ToDouble(obj["CurrentTemperature"]);
                    if (temp > 0 && temp < 120) return temp;
                }
            }
            catch { }
            return -1;
        }

        private void QueryWmiThermalZones(List<string> list)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\wmi", "select CurrentTemperature, InstanceName from MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double kelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                    double celsius = (kelvin / 10.0) - 273.15;
                    string name = obj["InstanceName"]?.ToString() ?? "ACPI Zone";
                    
                    // Format instance name nicely
                    if (name.Contains("_TZ_"))
                    {
                        name = name.Substring(name.IndexOf("_TZ_"));
                    }
                    
                    if (celsius > -20 && celsius < 150)
                    {
                        list.Add($"{name}: {celsius:F1}°C");
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
        }
    }
}
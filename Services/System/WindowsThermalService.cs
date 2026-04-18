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
        bool HasBattery);

    public class WindowsThermalService : IThermalService, IDisposable
    {
        private readonly ILibreHardwareService _libreService;

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
                                    else if (cpuTemp == -1) cpuTemp = sensor.Value ?? cpuTemp;
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
                        // GPU Telemetry
                        else if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Core"))
                                    gpuTemp = sensor.Value ?? gpuTemp;
                                else if (sensor.SensorType == SensorType.Fan)
                                    fanSpeed = sensor.Value ?? fanSpeed;
                            }
                        }
                        // Motherboard/LPC Fans
                        else if (hardware.HardwareType == HardwareType.Motherboard)
                        {
                            foreach (var subHardware in hardware.SubHardware)
                            {
                                foreach (var sensor in subHardware.Sensors)
                                {
                                    if (sensor.SensorType == SensorType.Fan && fanSpeed == 0)
                                        fanSpeed = sensor.Value ?? fanSpeed;
                                }
                            }
                        }
                    }
                }
                catch { }

                string power = "Detecting...";
                int bat = 0;
                string status = "Unknown";
                bool hasBattery = false;

                try
                {
                    using var searcher = new ManagementObjectSearcher("select EstimatedChargeRemaining, BatteryStatus from Win32_Battery");
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

                return new ThermalSnapshot(cpuTemp, gpuTemp, cpuPower, cpuVoltage, fanSpeed, power, bat, status, hasBattery);
            });
        }

        public void Dispose()
        {
            // Shared service handles disposal
        }
    }
}
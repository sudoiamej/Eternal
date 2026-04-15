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

    public record ThermalSnapshot(double CpuTemp, string PowerSource, int BatteryPercent, string BatteryStatus, bool HasBattery);

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
                double maxTemp = -1;
                
                try
                {
                    _libreService.Update();
                    foreach (IHardware hardware in _libreService.Computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    if (sensor.Name.Contains("Core Max") || sensor.Name.Contains("Package"))
                                    {
                                        maxTemp = sensor.Value ?? maxTemp;
                                        break; 
                                    }
                                    if (maxTemp == -1) maxTemp = sensor.Value ?? maxTemp;
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

                return new ThermalSnapshot(maxTemp, power, bat, status, hasBattery);
            });
        }

        public void Dispose()
        {
            // Shared service handles disposal
        }
    }
}
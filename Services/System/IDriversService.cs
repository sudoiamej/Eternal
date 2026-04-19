using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Linq;

namespace Eternal.Services.System
{
    public interface IDriversService
    {
        Task<List<DriverInfo>> GetInstalledDriversAsync();
        Task<OemSupportInfo> GetOemSupportInfoAsync();
    }

    public record DriverInfo(string Name, string Description, string Version, string Provider, string Type, bool IsSigned, string HardwareId);
    public record OemSupportInfo(string Vendor, string SerialNumber);

    public class WindowsDriversService : IDriversService
    {
        public Task<List<DriverInfo>> GetInstalledDriversAsync()
        {
            return Task.Run(() =>
            {
                var drivers = new List<DriverInfo>();
                try
                {
                    // Target PnPSignedDriver for version and HWID, and PnPEntity for Name/Manufacturer
                    // We join them conceptually by searching for common IDs
                    using var searcher = new ManagementObjectSearcher("select DeviceName, Manufacturer, Description, DriverVersion, DeviceID from Win32_PnPSignedDriver");

                    foreach (var obj in searcher.Get())
                    {
                        try 
                        {
                            string name = obj["DeviceName"]?.ToString() ?? "Unknown Device";
                            string provider = obj["Manufacturer"]?.ToString() ?? "Unknown";
                            string description = obj["Description"]?.ToString() ?? "System Managed Device";
                            string version = obj["DriverVersion"]?.ToString() ?? "N/A";
                            string hwid = obj["DeviceID"]?.ToString() ?? "N/A";

                            // Device Manager classification logic
                            string type = "3rd Party";
                            if (provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                provider.Contains("(Standard", StringComparison.OrdinalIgnoreCase))
                            {
                                type = "System (Microsoft)";
                            }

                            drivers.Add(new DriverInfo(name, description, version, provider, type, true, hwid));
                        } catch { }
                    }                }
                catch { }
                
                return drivers.OrderBy(d => d.Name).ToList();
            });
        }

        public Task<OemSupportInfo> GetOemSupportInfoAsync()
        {
            return Task.Run(() =>
            {
                string vendor = "Unknown";
                string serial = "Unknown";

                try
                {
                    using (var searcher = new ManagementObjectSearcher("select Manufacturer, IdentifyingNumber from Win32_ComputerSystemProduct"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            vendor = obj["Manufacturer"]?.ToString() ?? vendor;
                            serial = obj["IdentifyingNumber"]?.ToString() ?? serial;
                            break;
                        }
                    }

                    if (serial == "To be filled by O.E.M." || string.IsNullOrEmpty(serial))
                    {
                        using (var searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BIOS"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                serial = obj["SerialNumber"]?.ToString() ?? serial;
                                break;
                            }
                        }
                    }
                }
                catch { }

                return new OemSupportInfo(vendor, serial);
            });
        }
    }
}
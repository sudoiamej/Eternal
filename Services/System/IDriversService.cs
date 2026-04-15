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
    }

    public record DriverInfo(string Name, string Description, string Version, string Provider, string Type, bool IsSigned);

    public class WindowsDriversService : IDriversService
    {
        public Task<List<DriverInfo>> GetInstalledDriversAsync()
        {
            return Task.Run(() =>
            {
                var drivers = new List<DriverInfo>();
                try
                {
                    // optimized query targeting PnPEntity (Matches devmgmt.msc behavior)
                    // We avoid 'select *' to reduce WMI overhead
                    using var searcher = new ManagementObjectSearcher("select Name, Manufacturer, Description, Status from Win32_PnPEntity");

                    foreach (var obj in searcher.Get())
                    {
                        try 
                        {
                            string name = obj["Name"]?.ToString();
                            if (string.IsNullOrEmpty(name)) continue;

                            string provider = obj["Manufacturer"]?.ToString() ?? "Unknown";
                            string description = obj["Description"]?.ToString() ?? "System Managed Device";

                            // Device Manager classification logic
                            string type = "3rd Party";
                            if (provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                provider.Contains("(Standard", StringComparison.OrdinalIgnoreCase))
                            {
                                type = "System (Microsoft)";
                            }

                            drivers.Add(new DriverInfo(name, description, "Active", provider, type, true));
                        } catch { }
                    }                }
                catch { }
                
                return drivers.OrderBy(d => d.Name).ToList();
            });
        }
    }
}
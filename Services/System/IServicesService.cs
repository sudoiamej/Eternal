using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Linq;

namespace Eternal.Services.System
{
    public interface IServicesService
    {
        Task<List<ServiceInfo>> GetServicesAsync();
    }

    public record ServiceInfo(string Name, string DisplayName, string Status, string StartupType, string LogOnAs, string Description, string Type);

    public class WindowsServicesService : IServicesService
    {
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public Task<List<ServiceInfo>> GetServicesAsync()
        {
            return Task.Run(() =>
            {
                var services = new List<ServiceInfo>();
                try
                {
                    // Query Win32_Service (Matches services.msc behavior)
                    using var searcher = CreateSearcher("select Name, DisplayName, State, StartMode, StartName, Description from Win32_Service");

                    foreach (var obj in searcher.Get())
                    {
                        try 
                        {
                            string name = obj["Name"]?.ToString() ?? "";
                            string displayName = obj["DisplayName"]?.ToString() ?? name;
                            string state = obj["State"]?.ToString() ?? "Unknown";
                            string startMode = obj["StartMode"]?.ToString() ?? "Unknown";
                            string logOnAs = obj["StartName"]?.ToString() ?? "Unknown";
                            string description = obj["Description"]?.ToString() ?? "";

                            // services.msc classification logic
                            string type = "3rd Party";
                            if (logOnAs.Contains("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                                logOnAs.Contains("LocalService", StringComparison.OrdinalIgnoreCase) ||
                                logOnAs.Contains("NetworkService", StringComparison.OrdinalIgnoreCase))
                            {
                                type = "System";
                            }

                            services.Add(new ServiceInfo(name, displayName, state, startMode, logOnAs, description, type));
                        } catch { }
                    }
                }
                catch { }
                
                return services.OrderBy(s => s.DisplayName).ToList();
            });
        }
    }
}

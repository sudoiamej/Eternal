using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;

namespace Eternal.Services.System
{
    public interface IServicesService
    {
        Task<List<ServiceInfo>> GetServicesAsync();
        Task<bool> ToggleDelayedStartAsync(string serviceName, bool delayed);
    }

    public record ServiceInfo(
        string Name, 
        string DisplayName, 
        string Status, 
        string StartupType, 
        string LogOnAs, 
        string Description, 
        string Type,
        bool IsDelayed
    );

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

                            // Query registry for delayed autostart
                            bool isDelayed = false;
                            try
                            {
                                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}"))
                                {
                                    if (key != null)
                                     {
                                         var val = key.GetValue("DelayedAutostart");
                                         if (val != null && int.TryParse(val.ToString(), out int delayVal) && delayVal == 1)
                                         {
                                             isDelayed = true;
                                         }
                                     }
                                }
                            }
                            catch { }

                            string type = "3rd Party";
                            if (logOnAs.Contains("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                                logOnAs.Contains("LocalService", StringComparison.OrdinalIgnoreCase) ||
                                logOnAs.Contains("NetworkService", StringComparison.OrdinalIgnoreCase))
                            {
                                type = "System";
                            }

                            services.Add(new ServiceInfo(name, displayName, state, startMode, logOnAs, description, type, isDelayed));
                        } catch { }
                    }
                }
                catch { }
                
                return services.OrderBy(s => s.DisplayName).ToList();
            });
        }

        public async Task<bool> ToggleDelayedStartAsync(string serviceName, bool delayed)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string keyPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath, true))
                    {
                        if (key == null) return false;
                        
                        if (delayed)
                        {
                            key.SetValue("Start", 2, RegistryValueKind.DWord);
                            key.SetValue("DelayedAutostart", 1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            key.SetValue("DelayedAutostart", 0, RegistryValueKind.DWord);
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"Failed to set delayed start for {serviceName}: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

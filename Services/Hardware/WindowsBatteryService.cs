using System;
using System.Management;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Hardware
{
    public class WindowsBatteryService : IBatteryService
    {
        public async Task<BatteryInfo?> GetBatteryInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                    foreach (var obj in searcher.Get())
                    {
                        string status = obj["Status"]?.ToString() ?? "Unknown";
                        int charge = Convert.ToInt32(obj["EstimatedChargeRemaining"] ?? 0);
                        int designCap = Convert.ToInt32(obj["DesignCapacity"] ?? 0);
                        int fullCap = Convert.ToInt32(obj["FullChargeCapacity"] ?? 0);
                        int curCap = fullCap; // Win32_Battery doesn't always show real-time mAh curCap well
                        
                        double wear = 0;
                        if (designCap > 0)
                        {
                            wear = 100 - ((double)fullCap / designCap * 100);
                        }

                        string name = obj["Name"]?.ToString() ?? "Standard Battery";
                        string chemistry = obj["Chemistry"]?.ToString() ?? "Unknown";
                        string pwrSource = Convert.ToInt32(obj["BatteryStatus"] ?? 0) == 2 ? "AC Adapter" : "Battery";

                        return new BatteryInfo(
                            status, charge, pwrSource, wear, designCap, fullCap, curCap, 0, chemistry, name
                        );
                    }
                }
                catch { }
                return null;
            });
        }
    }
}

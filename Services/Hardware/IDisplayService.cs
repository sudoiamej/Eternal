using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Hardware
{
    public interface IDisplayService
    {
        Task<List<MonitorInfo>> GetMonitorsAsync();
        Task<List<DisplayAdapter>> GetAdaptersAsync();
        Task<bool> ApplyDisplaySettingsAsync(MonitorInfo monitor, int width, int height, int refreshRate);
        Task<bool> IdentifyMonitorAsync(MonitorInfo monitor);
    }
}

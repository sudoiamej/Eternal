using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public class MonitorInfo
    {
        public string Name { get; set; } = "Generic Monitor";
        public string DeviceID { get; set; } = string.Empty;
        public string Model { get; set; } = "N/A";
        public int Index { get; set; }
        public int CurrentWidth { get; set; }
        public int CurrentHeight { get; set; }
        public int RefreshRate { get; set; }
        public int Scaling { get; set; } = 100;
        public bool IsPrimary { get; set; }
        public bool IsHdrEnabled { get; set; }
        public string Orientation { get; set; } = "Landscape";
        public string GPU { get; set; } = "System Graphics";
        public string ConnectionType { get; set; } = "HDMI / DP";
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        
        public List<ResolutionPreset> SupportedResolutions { get; } = new();
    }

    public record ResolutionPreset(int Width, int Height, List<int> RefreshRates);

    public class DisplayAdapter
    {
        public string Name { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public long VramBytes { get; set; }
        public string Status { get; set; } = "Operational";
    }
}

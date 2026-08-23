using System;

namespace Eternal.Models
{
    public class PortScanResultModel
    {
        public int Port { get; set; }
        public string ServiceName { get; set; } = "Unknown";
        public string State { get; set; } = "Closed";
        public string RiskLevel { get; set; } = "Low";
        public string StateColor { get; set; } = "#888896";
    }
}

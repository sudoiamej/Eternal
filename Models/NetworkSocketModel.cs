using System;

namespace Eternal.Models
{
    public class NetworkSocketModel
    {
        public string Protocol { get; set; } = "TCP";
        public string LocalEndpoint { get; set; } = string.Empty;
        public string RemoteEndpoint { get; set; } = string.Empty;
        public string State { get; set; } = "Established";
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Unknown";
        public string StateColor { get; set; } = "#10B981";
    }
}

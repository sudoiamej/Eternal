using System;

namespace Eternal.Models
{
    public class DriverKernelModel
    {
        public string DriverName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PathName { get; set; } = string.Empty;
        public string State { get; set; } = "Running";
        public string StartMode { get; set; } = "System";
        public string StateColor { get; set; } = "#10B981";
    }
}

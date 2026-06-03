using System;

namespace Eternal.Models
{
    public class BootRecord
    {
        public string Identifier { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public string Inherit { get; set; } = string.Empty;
        public string OsDevice { get; set; } = string.Empty;
        public string SystemRoot { get; set; } = string.Empty;
        public string ResumeObject { get; set; } = string.Empty;
        public string Nx { get; set; } = string.Empty;
        public string BootMenuPolicy { get; set; } = string.Empty;
        public string SafeBoot { get; set; } = string.Empty;
        
        // Helper to determine the type for UI icons
        public bool IsBootManager => Identifier?.Contains("{bootmgr}", StringComparison.OrdinalIgnoreCase) ?? false;
        public bool IsCurrent => Identifier?.Contains("{current}", StringComparison.OrdinalIgnoreCase) ?? false;
        public bool IsSafeBoot => !string.IsNullOrEmpty(SafeBoot);
    }
}

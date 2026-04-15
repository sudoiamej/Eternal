using System;

namespace Eternal.Models
{
    public class BootRecord
    {
        public string Identifier { get; set; }
        public string Device { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public string Locale { get; set; }
        public string Inherit { get; set; }
        public string OsDevice { get; set; }
        public string SystemRoot { get; set; }
        public string ResumeObject { get; set; }
        public string Nx { get; set; }
        public string BootMenuPolicy { get; set; }
        
        // Helper to determine the type for UI icons
        public bool IsBootManager => Identifier?.Contains("{bootmgr}", StringComparison.OrdinalIgnoreCase) ?? false;
        public bool IsCurrent => Identifier?.Contains("{current}", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}

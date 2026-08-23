using System;

namespace Eternal.Models
{
    public class InstalledAppModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Publisher { get; set; } = "Unknown";
        public string DisplayVersion { get; set; } = "N/A";
        public string InstallDate { get; set; } = "N/A";
        public string UninstallString { get; set; } = string.Empty;
        public string Architecture { get; set; } = "x64";
    }
}

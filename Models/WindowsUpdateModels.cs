using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public enum WindowsUpdateStatus { Available, Installed, Optional, Hidden }

    public class WindowsUpdateItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string KBArticle { get; set; } = string.Empty;
        public string UpdateID { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsDownloaded { get; set; }
        public WindowsUpdateStatus Status { get; set; }
        public string SupportUrl { get; set; } = string.Empty;
    }

    public class WindowsLifecycleInfo
    {
        public string Edition { get; set; } = string.Empty;
        public string DisplayVersion { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public DateTime EolDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Status { get; set; } = "SUPPORTED";
        public bool IsEol => DaysRemaining <= 0;
    }
}

using System;

namespace Eternal.Models
{
    public enum IssueSeverity { Required, Recommended, Optional }
    public enum ScannerActionType { AutoFix, ManualNavigation }
    public enum ScannerSortOption { Default, Level, EasyToHard, Alphabetical, SafeToDangerous }

    public class ScannerIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public ScannerActionType ActionType { get; set; }
        public string ActionTarget { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
    }
}

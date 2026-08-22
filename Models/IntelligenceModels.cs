using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Eternal.Models
{
    public enum SeverityLevel { Info, Low, Medium, High, Critical }
    public enum TrustLevel { Safe, Warning, Critical, Unknown }

    public record HumanExplanation(string Title, string Content, SeverityLevel Severity)
    {
        public string PlainLanguage => Content;
    }

    public record Anomaly(string Category, string Description, SeverityLevel Severity, HumanExplanation Explanation)
    {
        public string Title => Category;
        public string PlainLanguage => Explanation?.Content ?? Description;
    }
    
    public record TrustScore(
        int Value, 
        string Label, 
        string ColorHex, 
        TrustLevel Level, 
        string Explanation, 
        string Status,
        int OverallIndex = 0,
        int StartupScore = 0,
        int DriverScore = 0,
        int SystemFileScore = 0,
        int NetworkScore = 0)
    {
        // Constructor for WindowsIntelligenceService
        public TrustScore(int value, TrustLevel level, string explanation, int startup, int driver, int sysFile, int network)
            : this(value, level.ToString(), GetColor(value), level, explanation, level.ToString(), value, startup, driver, sysFile, network)
        {
        }

        // Dashboard legacy constructor
        public TrustScore(int Value, string Label, string ColorHex, string Status, int StartupScore, int DriverScore, int SystemFileScore, int NetworkScore, string Explanation)
            : this(Value, Label, ColorHex, TrustLevel.Unknown, Explanation, Status, Value, StartupScore, DriverScore, SystemFileScore, NetworkScore)
        {
        }

        private static string GetColor(int val) => val > 80 ? "#4CAF50" : (val > 50 ? "#FFC107" : "#F44336");
    }

    public record RootCause(string Component, string Issue, string Impact, HumanExplanation Explanation)
    {
        public string Resolution => Explanation?.Content ?? "";
    }

    public record ExtendedProcessInfo(int PID, string Name, List<string> StaticImports, List<string> LoadedModules, List<string> HeuristicReasons);
    public record LogEntry(DateTime Timestamp, string Source, string Message, string Level = "Info");
    public record NetworkConnection(string Protocol, string LocalAddress, string RemoteAddress, string State, int PID, string ProcessName);
    public record PropertyItem(string Name, string Value);

    public enum ProcessCategory { Apps, Background, Windows }

    public partial class ProcessDetail : ObservableObject
    {
        public int PID { get; }
        public string Name { get; }
        [ObservableProperty] private double _cpuUsage;
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(MemoryUsage))] 
        private long _memoryBytes;
        public string Path { get; }
        public bool IsSigned { get; }
        [ObservableProperty] private string _impact;
        [ObservableProperty] private string _status;
        public int SessionId { get; }
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(DiskUsage))] 
        private long _diskBytesPerSec;
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(NetworkUsage))] 
        private long _networkBytesPerSec;
        public ProcessCategory Category { get; }

        public ProcessDetail(int PID, string Name, double CpuUsage, long MemoryBytes, string Path, bool IsSigned, string Impact, string Status, int SessionId, long DiskBytesPerSec = 0, long NetworkBytesPerSec = 0, ProcessCategory Category = ProcessCategory.Background)
        {
            this.PID = PID;
            this.Name = Name;
            this.CpuUsage = CpuUsage;
            this.MemoryBytes = MemoryBytes;
            this.Path = Path;
            this.IsSigned = IsSigned;
            this.Impact = Impact;
            this.Status = Status;
            this.SessionId = SessionId;
            this.DiskBytesPerSec = DiskBytesPerSec;
            this.NetworkBytesPerSec = NetworkBytesPerSec;
            this.Category = Category;
        }

        public string MemoryUsage => (MemoryBytes / 1024.0 / 1024.0).ToString("F1") + " MB";
        public string DiskUsage => (DiskBytesPerSec / 1024.0 / 1024.0).ToString("F2") + " MB/s";
        public string NetworkUsage => (NetworkBytesPerSec / 1024.0 / 1024.0).ToString("F2") + " MB/s";
    }

    public partial class ProcessGroup : ObservableObject
    {
        public string Name { get; }
        public ProcessCategory Category { get; }
        public ObservableCollection<ProcessDetail> Processes { get; } = new();
        [ObservableProperty] private bool _isExpanded;

        public ProcessGroup(string name, IEnumerable<ProcessDetail> processes, ProcessCategory category)
        {
            Name = name;
            Category = category;
            foreach (var p in processes) Processes.Add(p);
        }

        public long TotalMemory => Processes.Sum(i => i.MemoryBytes);
        public double TotalCpu => Processes.Sum(i => i.CpuUsage);
        public long TotalDisk => Processes.Sum(i => i.DiskBytesPerSec);
        public long TotalNetwork => Processes.Sum(i => i.NetworkBytesPerSec);
        public int Count => Processes.Count;
        public string Impact => Processes.Any(i => i.Impact == "High") ? "High" : (Processes.Any(i => i.Impact == "Medium") ? "Medium" : "Low");
    }

    public partial class CategoryGroup : ObservableObject
    {
        public ProcessCategory Category { get; }
        public ObservableCollection<ProcessGroup> Groups { get; } = new();
        [ObservableProperty] private bool _isExpanded;

        public CategoryGroup(ProcessCategory category, IEnumerable<ProcessGroup> groups)
        {
            Category = category;
            foreach (var g in groups) Groups.Add(g);
            IsExpanded = category == ProcessCategory.Apps; // Apps expanded by default
        }

        public string Name => Category switch {
            ProcessCategory.Apps => "Apps",
            ProcessCategory.Background => "Background processes",
            ProcessCategory.Windows => "Windows processes",
            _ => "Other"
        };
        public int TotalCount => Groups.Sum(g => g.Count);
    }

    public enum DashboardLayoutMode
    {
        Grid,
        List
    }

    public class AppSettings
    {
        public bool IsFirstRun { get; set; } = true;
        public DashboardLayoutMode DashboardLayoutMode { get; set; } = DashboardLayoutMode.Grid;
        public string AppVersion { get; set; } = "3.0.0";
        public bool UseLegacyUI { get => false; set { } }
        public bool UseNeumorphicUI { get; set; } = false;
        public string NewUiGradiency { get; set; } = "Deep Space";
        public string Theme { get; set; } = "Dark";
        public string ThemeAccentColor { get; set; } = "#7F00FF";
        public bool IsAutoUpdateEnabled { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public bool IsAdvancedMode { get; set; } = false;
        public bool IsSidebarExpanded { get; set; } = true;
        public List<string> DisabledFeatures { get; set; } = new();
        public List<string> PinnedFeatures { get; set; } = new() { "Processes", "Storage", "Dashboard" };

        // System Flags (Developer / Testing)
        public bool EnableWmiPolling { get; set; } = true;
        public bool BypassAdminCheck { get; set; } = false;
        public bool ForcePeMode { get; set; } = false;
        public bool SafeExecutionMode { get; set; } = true; 
        public bool VerboseServiceLogging { get; set; } = false; 
        public bool SimulateUpdateFailure { get; set; } = false;
        public bool UseNativeMemoryPolling { get; set; } = true;

        // Security & Environment
        public bool IsStartupLockEnabled { get; set; } = false;
        public bool UseWindowsHelloPin { get; set; } = true;
        public int FailedAttemptsCount { get; set; } = 0;
        public int CurrentLockoutMinutes { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public bool IsVerboseLoggingEnabled { get; set; } = false;
        public bool RunAtStartup { get; set; } = false;
        public string MachineFingerprint { get; set; } = "Unknown";
        
        // Monitoring & Performance
        public double FontAdjustmentScale { get; set; } = 1.0;
        public double WindowScale { get; set; } = 1.0;
        public int RefreshFrequency { get; set; } = 1000;
        public bool PreloadOnStartup { get; set; } = true;
        public string PollingProfile { get; set; } = "Balanced";
        public int WmiTimeoutSeconds { get; set; } = 5;

        // Paths
        public string ExportFolderPath { get; set; } = string.Empty;
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
        public bool IsFactoryResetPending { get; set; } = false;
    }
}

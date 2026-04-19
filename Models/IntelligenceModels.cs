using System;
using System.Collections.Generic;
using System.Linq;

namespace Eternal.Models
{
    public enum SeverityLevel { Info, Low, Medium, High, Critical }
    public enum TrustLevel { Safe, Warning, Critical, Unknown }

    public record HumanExplanation(string TechnicalValue, string PlainLanguage, SeverityLevel Impact);

    public record Anomaly(string Component, string Description, SeverityLevel Severity, HumanExplanation Explanation);

    public record TrustScore(int OverallIndex, TrustLevel Status, string Explanation, 
                             int StartupScore, int DriverScore, int SystemFileScore, int NetworkScore);

    public record RootCause(string Component, string ProcessName, string Reason, HumanExplanation Explanation);

    public record NetworkConnection(string Protocol, string LocalAddress, string RemoteAddress, string State, int ProcessId, string ProcessName);

    public record ProcessDetail(int Id, string Name, double CpuUsage, long MemoryBytes, string Path, bool IsSigned, string Impact);

    public record ProcessGroup(string Name, List<ProcessDetail> Items)
    {
        public long TotalMemory => Items.Sum(i => i.MemoryBytes);
        public int Count => Items.Count;
        public string Impact => Items.Any(i => i.Impact == "High") ? "High" : (Items.Any(i => i.Impact == "Medium") ? "Medium" : "Low");
    }
    
    public record PropertyItem(string Key, string Value);

    public record ExtendedProcessInfo(
        int Id,
        string Name,
        List<string> StaticImports,
        List<string> LoadedModules,
        List<string> HeuristicReasons
    );

    public record LogEntry(DateTime Timestamp, string Message, string Level = "INFO");

    public class AppSettings
    {
        public int RefreshFrequency { get; set; } = 2;
        public bool PreloadOnStartup { get; set; } = true;
        public bool IsAdvancedMode { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public string ThemeAccentColor { get; set; } = "#9B59B6";

        // 1. Telemetry & Performance
        public string PollingProfile { get; set; } = "Balanced"; // Balanced, High, PowerSaver
        public bool RunAtStartup { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public string? MachineFingerprint { get; set; }

        // 3. Data & Privacy
        public string ExportFolderPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public int WmiTimeoutSeconds { get; set; } = 10;
        public bool IsVerboseLoggingEnabled { get; set; } = true;

        // 4. App Updates
        public bool IsAutoUpdateEnabled { get; set; } = false;
        public DateTime? LastUpdateCheck { get; set; }

        // 5. Entry Lock
        public bool IsStartupLockEnabled { get; set; } = false;
        public string StartupLockPin { get; set; } = "120076";
        public DateTime? LockoutEnd { get; set; }
        public int FailedAttemptsCount { get; set; } = 0;
        public int CurrentLockoutMinutes { get; set; } = 0;
    }
}
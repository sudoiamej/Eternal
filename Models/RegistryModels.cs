using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Eternal.Models
{
    public enum RegistryHiveType
    {
        ClassesRoot,
        CurrentUser,
        LocalMachine,
        Users,
        CurrentConfig
    }

    public class RegistryValueInfo
    {
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public RegistryValueKind Kind { get; set; }
        public string Description { get; set; } = "No description available.";
        
        // HCI: Simplified interpretation of the value
        public string Summary => GetSummary();

        private string GetSummary()
        {
            if (Value == null) return "Null";
            
            return Kind switch
            {
                RegistryValueKind.DWord => $"(DWORD) {Value} (0x{Convert.ToInt32(Value):X8})",
                RegistryValueKind.QWord => $"(QWORD) {Value}",
                RegistryValueKind.String => $"(String) {Value}",
                RegistryValueKind.ExpandString => $"(ExpandString) {Value}",
                RegistryValueKind.MultiString => $"(MultiString) {string.Join(", ", (string[])Value)}",
                RegistryValueKind.Binary => $"(Binary) {BitConverter.ToString((byte[])Value).Replace("-", " ")}",
                _ => Value.ToString() ?? "Unknown"
            };
        }
    }

    public class RegistryKeyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public RegistryHiveType Hive { get; set; }
        public List<string> SubKeys { get; set; } = new List<string>();
        public List<RegistryValueInfo> Values { get; set; } = new List<RegistryValueInfo>();
    }

    public class RegistryTweakDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Hive { get; set; } = string.Empty;
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public RegistryValueKind Kind { get; set; }
        public List<RegistryOption> Options { get; set; } = new List<RegistryOption>();

        // Compatibility Flags
        public bool IsWin10Compatible { get; set; } = true;
        public bool IsWin11Compatible { get; set; } = true;
        public bool IsCurrentOSCompatible { get; set; } = true;
    }

    public class RegistryOption
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
        public string Impact { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public RegistryTweakDefinition? Parent { get; set; }
    }

    // FEATURE 2: Undo Vault Entry
    public class RegistryUndoEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Hive { get; set; } = string.Empty;
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public object OriginalValue { get; set; } = null!;
        public RegistryValueKind Kind { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // FEATURE 3: Watchlist / Drift Entry
    public partial class RegistryWatchEntry : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Hive { get; set; } = string.Empty;
        public string KeyPath { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public object BaselineValue { get; set; } = null!;
        
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private object _currentValue = null!;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool _isDrifting;

        public string DisplayPath => $"{Hive}\\{KeyPath}";
    }

    // FEATURE 4: Provenance / Ownership
    public class RegistryProvenance
    {
        public string OwnerName { get; set; } = "Generic System Component";
        public string BinaryPath { get; set; } = string.Empty;
        public string Publisher { get; set; } = "Microsoft Corporation";
        public bool IsSystemComponent { get; set; } = true;
        public string IconPath { get; set; } = string.Empty;
    }
}

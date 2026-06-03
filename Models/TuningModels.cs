using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Models
{
    public enum TweakCategory { Privacy, Performance, SystemUI, Services }

    public class SystemTweak
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TweakCategory Category { get; set; }
        public bool IsApplied { get; set; }
        public string RegistryPath { get; set; } = string.Empty;
        public string RegistryValue { get; set; } = string.Empty;
        public object AppliedValue { get; set; } = default!;
        public object DefaultValue { get; set; } = default!;
        public int? MinBuild { get; set; }
        public int? MaxBuild { get; set; }
        public Microsoft.Win32.RegistryValueKind ValueKind { get; set; } = Microsoft.Win32.RegistryValueKind.DWord;
    }
}

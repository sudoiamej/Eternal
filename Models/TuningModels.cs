using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Models
{
    public enum TweakCategory { Privacy, Performance, SystemUI, Services }

    public class SystemTweak
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TweakCategory Category { get; set; }
        public bool IsApplied { get; set; }
        public string RegistryPath { get; set; }
        public string RegistryValue { get; set; }
        public object AppliedValue { get; set; }
        public object DefaultValue { get; set; }
        public int? MinBuild { get; set; }
        public int? MaxBuild { get; set; }
        public Microsoft.Win32.RegistryValueKind ValueKind { get; set; } = Microsoft.Win32.RegistryValueKind.DWord;
    }
}

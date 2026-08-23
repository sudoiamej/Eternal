using System;

namespace Eternal.Models
{
    public class ProcessModuleModel
    {
        public string ModuleName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string BaseAddress { get; set; } = "0x00000000";
        public string MemorySizeFormatted { get; set; } = "0 KB";
        public string Version { get; set; } = "N/A";
    }
}

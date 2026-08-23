using System;

namespace Eternal.Models
{
    public class HexRowModel
    {
        public string Offset { get; set; } = "00000000";
        public string HexBytes { get; set; } = string.Empty;
        public string AsciiString { get; set; } = string.Empty;
    }
}

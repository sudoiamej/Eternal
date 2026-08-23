using System;

namespace Eternal.Models
{
    public class ProcessItemModel
    {
        public int Pid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WorkingSetFormatted { get; set; } = "0 KB";
    }
}

using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public class WimImageInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Created { get; set; } = string.Empty;
    }

    public class WimFileDetails
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public List<WimImageInfo> Images { get; set; } = new();
    }
}

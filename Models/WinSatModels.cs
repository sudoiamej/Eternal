using System;

namespace Eternal.Models
{
    public class WinSatScore
    {
        public double CpuScore { get; set; }
        public double MemoryScore { get; set; }
        public double DiskScore { get; set; }
        public double GraphicsScore { get; set; }
        public double D3DScore { get; set; }
        public double BaseScore { get; set; }
        public string AssessmentDate { get; set; } = "Unknown";
    }
}

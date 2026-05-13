using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class IntegrityResult
    {
        public string FeatureName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = "OK";
        public string Latency { get; set; } = "0ms";
    }

    public interface IFeatureIntegrityService
    {
        Task<List<IntegrityResult>> RunFullDiagnosticAsync();
    }
}

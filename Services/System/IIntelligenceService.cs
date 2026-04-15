using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IIntelligenceService
    {
        Task<List<Anomaly>> GetSystemAnomaliesAsync();
        Task<TrustScore> CalculateTrustScoreAsync();
        Task<List<RootCause>> GetPerformanceRootCausesAsync();
    }
}

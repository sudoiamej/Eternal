using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public class LexiconItemDelta
    {
        public string Theme { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public bool IsDrifted { get; set; }
    }

    public interface IRegistryLexiconService
    {
        Task<List<LexiconItemDelta>> AnalyzeSystemDriftAsync();
        Task RealignSystemAsync(List<LexiconItemDelta> deltas);
    }
}

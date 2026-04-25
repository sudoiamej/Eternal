using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface INeuralAdvisorService
    {
        bool IsModelAvailable { get; }
        bool IsHardwareCompatible { get; }
        string CompatibilityMessage { get; }

        /// <summary>
        /// Analyzes system telemetry or user queries using the local LLM.
        /// </summary>
        IAsyncEnumerable<string> AskAdvisorAsync(string systemContext, string userQuery);

        /// <summary>
        /// Frees the model from memory.
        /// </summary>
        void UnloadModel();
    }
}

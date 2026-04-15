using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface ITuningService
    {
        Task<List<SystemTweak>> GetTweaksAsync();
        Task<bool> ApplyTweakAsync(string tweakId);
        Task<bool> UndoTweakAsync(string tweakId);
        Task<bool> CreateRestorePointAsync(string description);
    }
}

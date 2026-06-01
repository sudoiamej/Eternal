using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IBootService
    {
        Task<List<BootRecord>> GetBootRecordsAsync();
        Task<int> GetBootTimeoutAsync();
        Task<bool> SetBootTimeoutAsync(int seconds);
        Task<bool> ToggleSafeBootAsync(string identifier, bool enable);
        Task<bool> DeleteBootEntryAsync(string identifier);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IProcessService
    {
        Task<List<ProcessDetail>> GetRunningProcessesAsync();
        Task<bool> KillProcessAsync(int pid);
        Task<ExtendedProcessInfo> GetExtendedProcessInfoAsync(ProcessDetail process);
    }
}
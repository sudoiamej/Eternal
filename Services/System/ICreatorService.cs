using System.Threading.Tasks;
using System.Collections.Generic;

namespace Eternal.Services.System
{
    public interface ICreatorService
    {
        // Dev-Mode Registry Swapper
        Task<(bool Success, string Message)> ToggleDevModeAsync(bool enable);
        
        // Kernel-Level Service Profiles
        Task<(bool Success, string Message)> ApplyServiceProfileAsync(string profileName);
        
        // Port Warden
        Task<List<PortInfo>> GetActivePortsAsync();
        
        // The Force Killer
        Task<(bool Success, string Message)> IdentifyAndKillFileHandleAsync(string filePath);

        // Workflow Acceleration Tricks
        Task<List<string>> ValidateEnvironmentPathAsync();
        Task<(bool Success, string Message)> ToggleDevHostEntryAsync(bool enable);
        Task<(bool Success, string Message)> PurgeStandbyMemoryAsync();
        Task<(bool Success, string Message)> CreateDirectoryJunctionAsync(string source, string target);

        // Malware Hunter Tools
        Task<List<ProcessSecurityInfo>> GetUnsignedProcessesAsync();
        Task<(bool Success, string Message)> SuspendProcessAsync(int pid);
        Task<List<PersistenceEntry>> GetPersistenceEntriesAsync();
        Task<(bool Success, string Message)> RemovePersistenceEntryAsync(string location, string name);
        Task<(bool Success, string Message)> IsolateProcessNetworkAsync(int pid, bool block);
        Task<(bool Success, string Message)> EnableRansomGuardAsync(bool enable);
    }

    public class ProcessSecurityInfo
    {
        public int PID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsSigned { get; set; }
        public string Signer { get; set; }
        public string Description { get; set; } = "Suspicious activity detected";
    }

    public class PersistenceEntry
    {
        public string Location { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string Type { get; set; } // Registry, Task, Startup
    }

    public class PortInfo
    {
        public string Protocol { get; set; }
        public string LocalAddress { get; set; }
        public int Port { get; set; }
        public string ProcessName { get; set; }
        public int PID { get; set; }
    }
}

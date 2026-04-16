using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IRegistryService
    {
        Task<RegistryKeyInfo?> GetKeyAsync(string hive, string path);
        Task<List<RegistryValueInfo>> GetValuesAsync(string hive, string path);
        Task<bool> SetValueAsync(string hive, string path, string valueName, object value, RegistryValueKind kind);
        Task<List<RegistryTweakDefinition>> GetCommonTweaksAsync();
        Task<string> GetKeyDescriptionAsync(string path);
        
        // Advanced Intelligence Features
        Task<RegistryValueKind> GetValueKindAsync(string hive, string path, string valueName);
        Task<RegistryProvenance> GetProvenanceAsync(string hive, string path);
        Task<List<RegistryWatchEntry>> CheckWatchlistDriftAsync(List<RegistryWatchEntry> watchlist);
    }
}

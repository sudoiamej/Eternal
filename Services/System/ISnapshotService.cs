using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface ISnapshotService
    {
        Task<SystemSnapshot> CreateSnapshotAsync(string description);
        Task<List<SystemSnapshot>> GetSavedSnapshotsAsync();
        Task SaveSnapshotAsync(SystemSnapshot snapshot);
        Task DeleteSnapshotAsync(string id);
        List<SnapshotDiff> CompareSnapshots(SystemSnapshot oldSnapshot, SystemSnapshot newSnapshot);
    }
}

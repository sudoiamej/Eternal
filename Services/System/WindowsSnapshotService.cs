using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsSnapshotService : ISnapshotService
    {
        private readonly string _storagePath;
        private readonly IServicesService _servicesService;
        private readonly IRegistryService _registryService;

        public WindowsSnapshotService(IServicesService servicesService, IRegistryService registryService)
        {
            _servicesService = servicesService;
            _registryService = registryService;
            
            _storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Eternal", "Snapshots");
            
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        public async Task<SystemSnapshot> CreateSnapshotAsync(string description)
        {
            return await Task.Run(async () =>
            {
                var entries = new List<SnapshotEntry>();

                // 1. Capture Services
                var services = await _servicesService.GetServicesAsync();
                foreach (var s in services)
                {
                    entries.Add(new SnapshotEntry(s.Name, s.Status, "Service"));
                }

                // 2. Capture Startup Apps (HKCU & HKLM Run)
                CaptureStartupFromRegistry(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", entries);
                CaptureStartupFromRegistry(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", entries);

                // 3. Capture Environment Variables
                var envVars = Environment.GetEnvironmentVariables();
                foreach (string key in envVars.Keys)
                {
                    entries.Add(new SnapshotEntry(key, envVars[key]?.ToString() ?? "", "EnvVariable"));
                }

                return new SystemSnapshot(Guid.NewGuid().ToString(), DateTime.Now, description, entries);
            });
        }

        private void CaptureStartupFromRegistry(RegistryKey root, string subKey, List<SnapshotEntry> entries)
        {
            try
            {
                using var key = root.OpenSubKey(subKey);
                if (key == null) return;
                foreach (var valueName in key.GetValueNames())
                {
                    entries.Add(new SnapshotEntry(valueName, key.GetValue(valueName)?.ToString() ?? "", $"Startup ({root.Name})"));
                }
            }
            catch { }
        }

        public async Task<List<SystemSnapshot>> GetSavedSnapshotsAsync()
        {
            return await Task.Run(() =>
            {
                var snapshots = new List<SystemSnapshot>();
                if (!Directory.Exists(_storagePath)) return snapshots;

                foreach (var file in Directory.GetFiles(_storagePath, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var snapshot = JsonConvert.DeserializeObject<SystemSnapshot>(json);
                        if (snapshot != null) snapshots.Add(snapshot);
                    }
                    catch { }
                }
                return snapshots.OrderByDescending(s => s.Timestamp).ToList();
            });
        }

        public async Task SaveSnapshotAsync(SystemSnapshot snapshot)
        {
            await Task.Run(() =>
            {
                var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                var filePath = Path.Combine(_storagePath, $"{snapshot.Id}.json");
                File.WriteAllText(filePath, json);
            });
        }

        public async Task DeleteSnapshotAsync(string id)
        {
            await Task.Run(() =>
            {
                var filePath = Path.Combine(_storagePath, $"{id}.json");
                if (File.Exists(filePath)) File.Delete(filePath);
            });
        }

        public List<SnapshotDiff> CompareSnapshots(SystemSnapshot oldSnapshot, SystemSnapshot newSnapshot)
        {
            var diffs = new List<SnapshotDiff>();
            var oldMap = oldSnapshot.Entries.ToDictionary(e => $"{e.Category}:{e.Name}", e => e.Value);
            var newMap = newSnapshot.Entries.ToDictionary(e => $"{e.Category}:{e.Name}", e => e.Value);

            // Check for additions and modifications
            foreach (var kvp in newMap)
            {
                string key = kvp.Key;
                string newValue = kvp.Value;
                string name = key.Split(':')[1];
                string category = key.Split(':')[0];

                if (oldMap.TryGetValue(key, out string? oldValue))
                {
                    if (oldValue != newValue)
                        diffs.Add(new SnapshotDiff(name, oldValue, newValue, category, DiffType.Modified));
                    else
                        diffs.Add(new SnapshotDiff(name, oldValue, newValue, category, DiffType.Identical));
                }
                else
                {
                    diffs.Add(new SnapshotDiff(name, "", newValue, category, DiffType.Added));
                }
            }

            // Check for removals
            foreach (var kvp in oldMap)
            {
                if (!newMap.ContainsKey(kvp.Key))
                {
                    string name = kvp.Key.Split(':')[1];
                    string category = kvp.Key.Split(':')[0];
                    diffs.Add(new SnapshotDiff(name, kvp.Value, "", category, DiffType.Removed));
                }
            }

            return diffs;
        }
    }
}

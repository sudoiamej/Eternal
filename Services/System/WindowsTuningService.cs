using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsTuningService : ITuningService
    {
        private List<SystemTweak> _tweaks;

        public WindowsTuningService()
        {
            InitializeTweaks();
        }

        private void InitializeTweaks()
        {
            _tweaks = new List<SystemTweak>
            {
                new SystemTweak
                {
                    Id = "privacy_telemetry",
                    Name = "Disable OS Telemetry",
                    Description = "Stops Windows from sending anonymous usage data and feedback to Microsoft.",
                    Category = TweakCategory.Privacy,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    RegistryValue = "AllowTelemetry",
                    AppliedValue = 0,
                    DefaultValue = 1
                },
                new SystemTweak
                {
                    Id = "ui_bing_start",
                    Name = "Disable Bing in Start",
                    Description = "Prevents web search results from appearing when you search the Start menu.",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                    RegistryValue = "DisableSearchBoxSuggestions",
                    AppliedValue = 1,
                    DefaultValue = 0
                },
                new SystemTweak
                {
                    Id = "privacy_cortana",
                    Name = "Disable Cortana",
                    Description = "Completely disables the Cortana voice assistant and its background processes.",
                    Category = TweakCategory.Privacy,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    RegistryValue = "AllowCortana",
                    AppliedValue = 0,
                    DefaultValue = 1
                },
                new SystemTweak
                {
                    Id = "perf_hibernation",
                    Name = "Disable Hibernation",
                    Description = "Deletes the large hiberfil.sys file to save several GBs of SSD space.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power",
                    RegistryValue = "HibernateEnabled",
                    AppliedValue = 0,
                    DefaultValue = 1
                },
                new SystemTweak
                {
                    Id = "ui_menu_delay",
                    Name = "Instant Context Menus",
                    Description = "Reduces the delay for menus to appear when clicked or hovered.",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    RegistryValue = "MenuShowDelay",
                    AppliedValue = "0",
                    DefaultValue = "400"
                },
                new SystemTweak
                {
                    Id = "perf_indexing",
                    Name = "Disable Search Indexing",
                    Description = "Stops the background Windows Search indexer to save CPU and disk IO.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    RegistryValue = "PreventIndexingLowDiskSpaceMB",
                    AppliedValue = 1,
                    DefaultValue = 0
                },
                new SystemTweak
                {
                    Id = "privacy_edge_telemetry",
                    Name = "Disable Edge Telemetry",
                    Description = "Prevents Microsoft Edge from sending usage and browsing data.",
                    Category = TweakCategory.Privacy,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge",
                    RegistryValue = "MetricsReportingEnabled",
                    AppliedValue = 0,
                    DefaultValue = 1
                },
                new SystemTweak
                {
                    Id = "ui_win11_menus",
                    Name = "Classic Context Menus",
                    Description = "Restores the Windows 10 style context menus (Removes 'Show More Options').",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                    RegistryValue = "",
                    AppliedValue = "",
                    DefaultValue = null
                }
            };
        }

        public async Task<List<SystemTweak>> GetTweaksAsync()
        {
            return await Task.Run(() =>
            {
                foreach (var tweak in _tweaks)
                {
                    tweak.IsApplied = CheckIsApplied(tweak);
                }
                return _tweaks;
            });
        }

        private bool CheckIsApplied(SystemTweak tweak)
        {
            try
            {
                var val = Registry.GetValue(tweak.RegistryPath, tweak.RegistryValue, null);
                if (val == null) return false;
                return val.ToString() == tweak.AppliedValue.ToString();
            }
            catch { return false; }
        }

        public async Task<bool> ApplyTweakAsync(string tweakId)
        {
            var tweak = _tweaks.FirstOrDefault(t => t.Id == tweakId);
            if (tweak == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    SetRegistryValue(tweak.RegistryPath, tweak.RegistryValue, tweak.AppliedValue);
                    tweak.IsApplied = true;
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> UndoTweakAsync(string tweakId)
        {
            var tweak = _tweaks.FirstOrDefault(t => t.Id == tweakId);
            if (tweak == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    SetRegistryValue(tweak.RegistryPath, tweak.RegistryValue, tweak.DefaultValue);
                    tweak.IsApplied = false;
                    return true;
                }
                catch { return false; }
            });
        }

        private void SetRegistryValue(string path, string name, object value)
        {
            // Helper to handle the "HKEY_..." prefix from Registry.GetValue format
            string keyPath = path;
            RegistryKey baseKey = Registry.LocalMachine;

            if (path.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                baseKey = Registry.LocalMachine;
                keyPath = path.Substring("HKEY_LOCAL_MACHINE\\".Length);
            }
            else if (path.StartsWith("HKEY_CURRENT_USER"))
            {
                baseKey = Registry.CurrentUser;
                keyPath = path.Substring("HKEY_CURRENT_USER\\".Length);
            }

            using var key = baseKey.CreateSubKey(keyPath, true);
            if (key != null)
            {
                if (value is int i) key.SetValue(name, i, RegistryValueKind.DWord);
                else key.SetValue(name, value);
            }
        }

        public async Task<bool> CreateRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ManagementScope scope = new ManagementScope("\\\\.\\root\\default");
                    ManagementPath path = new ManagementPath("SystemRestore");
                    ObjectGetOptions options = new ObjectGetOptions();
                    
                    using (ManagementClass process = new ManagementClass(scope, path, options))
                    {
                        ManagementBaseObject inParams = process.GetMethodParameters("CreateRestorePoint");
                        inParams["Description"] = description;
                        inParams["RestorePointType"] = 100; // APPLICATION_INSTALL
                        inParams["EventType"] = 100;        // BEGIN_SYSTEM_CHANGE
                        
                        ManagementBaseObject outParams = process.InvokeMethod("CreateRestorePoint", inParams, null);
                        return true; // If it doesn't throw, we assume it's queued/started
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Restore point error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

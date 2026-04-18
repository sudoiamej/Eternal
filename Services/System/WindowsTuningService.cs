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
                // PRIVACY CATEGORY
                new SystemTweak
                {
                    Id = "privacy_telemetry",
                    Name = "Disable OS Telemetry",
                    Description = "Stops Windows from sending anonymous usage data and feedback to Microsoft.",
                    Category = TweakCategory.Privacy,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    RegistryValue = "AllowTelemetry",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
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
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
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
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new SystemTweak
                {
                    Id = "privacy_activity_history",
                    Name = "Disable Activity History",
                    Description = "Prevents Windows from collecting your activity history and syncing it to the cloud.",
                    Category = TweakCategory.Privacy,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                    RegistryValue = "PublishUserActivities",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },

                // PERFORMANCE CATEGORY
                new SystemTweak
                {
                    Id = "perf_hibernation",
                    Name = "Disable Hibernation",
                    Description = "Deletes hiberfil.sys to save several GBs of SSD space. Disables Fast Startup.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power",
                    RegistryValue = "HibernateEnabled",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new SystemTweak
                {
                    Id = "perf_indexing",
                    Name = "Disable Search Indexing",
                    Description = "Stops background indexing to save CPU and disk IO. Search will be slower.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                    RegistryValue = "PreventIndexingLowDiskSpaceMB",
                    AppliedValue = 1,
                    DefaultValue = 0,
                    ValueKind = RegistryValueKind.DWord
                },
                new SystemTweak
                {
                    Id = "perf_transparency",
                    Name = "Disable Transparency",
                    Description = "Disables acrylic/blur effects in the taskbar and windows for better GPU performance.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    RegistryValue = "EnableTransparency",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new SystemTweak
                {
                    Id = "perf_game_dvr",
                    Name = "Disable Game DVR",
                    Description = "Disables background game recording which can impact gaming performance.",
                    Category = TweakCategory.Performance,
                    RegistryPath = @"HKEY_CURRENT_USER\System\GameConfigStore",
                    RegistryValue = "GameDVR_Enabled",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },

                // SYSTEM UI CATEGORY
                new SystemTweak
                {
                    Id = "ui_bing_start",
                    Name = "Disable Bing in Start",
                    Description = "Prevents web search results from appearing when you search the Start menu.",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                    RegistryValue = "DisableSearchBoxSuggestions",
                    AppliedValue = 1,
                    DefaultValue = 0,
                    ValueKind = RegistryValueKind.DWord
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
                    DefaultValue = "400",
                    ValueKind = RegistryValueKind.String
                },
                new SystemTweak
                {
                    Id = "ui_win11_menus",
                    Name = "Classic Context Menus",
                    Description = "Restores Windows 10 style context menus. (Requires Explorer Restart).",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32",
                    RegistryValue = "",
                    AppliedValue = "",
                    DefaultValue = null,
                    ValueKind = RegistryValueKind.String
                },
                new SystemTweak
                {
                    Id = "ui_taskbar_align",
                    Name = "Align Taskbar Left",
                    Description = "Moves the Windows 11 taskbar icons to the left side like classic Windows.",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    RegistryValue = "TaskbarAl",
                    AppliedValue = 0,
                    DefaultValue = 1,
                    ValueKind = RegistryValueKind.DWord
                },
                new SystemTweak
                {
                    Id = "ui_startup_delay",
                    Name = "Disable Startup Delay",
                    Description = "Removes the artificial delay Windows adds to startup apps on login.",
                    Category = TweakCategory.SystemUI,
                    RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                    RegistryValue = "StartupDelayInMSec",
                    AppliedValue = 0,
                    DefaultValue = 1000,
                    ValueKind = RegistryValueKind.DWord
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
                // Special case for subkey existence (Classic Menus)
                if (tweak.Id == "ui_win11_menus")
                {
                    using var baseKey = OpenBaseKey(tweak.RegistryPath, false);
                    using var key = baseKey.OpenSubKey(GetRelativePath(tweak.RegistryPath));
                    return key != null;
                }

                var val = Registry.GetValue(tweak.RegistryPath, tweak.RegistryValue, null);
                if (val == null) return false;

                if (tweak.ValueKind == RegistryValueKind.DWord)
                {
                    return Convert.ToInt32(val) == Convert.ToInt32(tweak.AppliedValue);
                }
                
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
                    SetRegistryValue(tweak.RegistryPath, tweak.RegistryValue, tweak.AppliedValue, tweak.ValueKind);
                    tweak.IsApplied = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Apply error: {ex.Message}");
                    return false;
                }
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
                    if (tweak.Id == "ui_win11_menus")
                    {
                        using var baseKey = OpenBaseKey(tweak.RegistryPath, true);
                        baseKey.DeleteSubKeyTree(GetRelativePath(tweak.RegistryPath), false);
                    }
                    else if (tweak.DefaultValue == null)
                    {
                        using var baseKey = OpenBaseKey(tweak.RegistryPath, true);
                        using var key = baseKey.OpenSubKey(GetRelativePath(tweak.RegistryPath), true);
                        key?.DeleteValue(tweak.RegistryValue, false);
                    }
                    else
                    {
                        SetRegistryValue(tweak.RegistryPath, tweak.RegistryValue, tweak.DefaultValue, tweak.ValueKind);
                    }
                    tweak.IsApplied = false;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Undo error: {ex.Message}");
                    return false;
                }
            });
        }

        private void SetRegistryValue(string path, string name, object value, RegistryValueKind kind)
        {
            string keyPath = GetRelativePath(path);
            using RegistryKey baseKey = OpenBaseKey(path, true);
            
            // If name is empty and value is empty string, we are setting the (Default) value of a key
            // This is used for the Classic Context Menus tweak
            using var key = baseKey.CreateSubKey(keyPath, true);
            
            if (key != null)
            {
                if (value == null)
                {
                    // If value is null, we just ensure the key exists (handled by CreateSubKey)
                    return;
                }

                if (kind == RegistryValueKind.DWord)
                {
                    key.SetValue(name, Convert.ToInt32(value), RegistryValueKind.DWord);
                }
                else
                {
                    key.SetValue(name, value.ToString(), kind);
                }
            }
        }

        private RegistryKey OpenBaseKey(string fullPath, bool writable)
        {
            if (fullPath.StartsWith("HKEY_LOCAL_MACHINE")) return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            if (fullPath.StartsWith("HKEY_CURRENT_USER")) return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            if (fullPath.StartsWith("HKEY_CLASSES_ROOT")) return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith("HKEY_LOCAL_MACHINE\\")) return fullPath.Substring("HKEY_LOCAL_MACHINE\\".Length);
            if (fullPath.StartsWith("HKEY_CURRENT_USER\\")) return fullPath.Substring("HKEY_CURRENT_USER\\".Length);
            if (fullPath.StartsWith("HKEY_CLASSES_ROOT\\")) return fullPath.Substring("HKEY_CLASSES_ROOT\\".Length);
            return fullPath;
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
                        return true; 
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

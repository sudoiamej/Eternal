using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Eternal.Models;

namespace Eternal.Services.Security
{
    public class WindowsPrivacyService : IPrivacyService
    {
        private List<PrivacyPolicy> _policies = new();

        public WindowsPrivacyService()
        {
            InitializePolicies();
        }

        private void InitializePolicies()
        {
            _policies = new List<PrivacyPolicy>
            {
                new PrivacyPolicy { Id = "TEL_MAIN", Name = "Windows Telemetry", Description = "Prevents Windows from sending basic usage data to Microsoft.", Category = PrivacyCategory.Telemetry, RegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", ValueName = "AllowTelemetry", HardenedValue = 0, DefaultValue = 1 },
                new PrivacyPolicy { Id = "ADV_ID", Name = "Advertising ID", Description = "Prevents apps from using your advertising ID for tailored ads.", Category = PrivacyCategory.Advertising, RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", ValueName = "Enabled", HardenedValue = 0, DefaultValue = 1 },
                new PrivacyPolicy { Id = "APP_LOC", Name = "Location Access", Description = "Prevents apps from accessing your physical location.", Category = PrivacyCategory.Permissions, RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", ValueName = "Value", HardenedValue = "Deny", DefaultValue = "Allow" },
                new PrivacyPolicy { Id = "APP_MIC", Name = "Microphone Access", Description = "Prevents apps from accessing your microphone.", Category = PrivacyCategory.Permissions, RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone", ValueName = "Value", HardenedValue = "Deny", DefaultValue = "Allow" },
                new PrivacyPolicy { Id = "COR_SRCH", Name = "Cortana / Web Search", Description = "Disables web search results in the Windows Start menu.", Category = PrivacyCategory.Search, RegistryPath = @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer", ValueName = "DisableSearchBoxSuggestions", HardenedValue = 1, DefaultValue = 0 },
                new PrivacyPolicy { Id = "FEED_REQ", Name = "Feedback Frequency", Description = "Prevents Windows from asking for feedback.", Category = PrivacyCategory.Telemetry, RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules", ValueName = "NumberOfSIUFInPeriod", HardenedValue = 0, DefaultValue = 1 }
            };
        }

        public async Task<PrivacyAuditResult> RunAuditAsync()
        {
            return await Task.Run(() =>
            {
                int hardenedCount = 0;
                foreach (var policy in _policies)
                {
                    try
                    {
                        var currentVal = Registry.GetValue(policy.RegistryPath, policy.ValueName, null);
                        policy.IsHardened = currentVal != null && currentVal.ToString() == policy.HardenedValue.ToString();
                        if (policy.IsHardened) hardenedCount++;
                    }
                    catch { policy.IsHardened = false; }
                }

                int score = (int)((double)hardenedCount / _policies.Count * 100);
                return new PrivacyAuditResult(score, _policies.Select(p => new PrivacyPolicy { 
                    Id = p.Id, Name = p.Name, Description = p.Description, Category = p.Category, 
                    RegistryPath = p.RegistryPath, ValueName = p.ValueName, HardenedValue = p.HardenedValue, 
                    DefaultValue = p.DefaultValue, IsHardened = p.IsHardened 
                }).ToList());
            });
        }

        public async Task<bool> ApplyPolicyAsync(string policyId)
        {
            var policy = _policies.FirstOrDefault(p => p.Id == policyId);
            if (policy == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    Registry.SetValue(policy.RegistryPath, policy.ValueName, policy.HardenedValue);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> UndoPolicyAsync(string policyId)
        {
            var policy = _policies.FirstOrDefault(p => p.Id == policyId);
            if (policy == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    Registry.SetValue(policy.RegistryPath, policy.ValueName, policy.DefaultValue);
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<bool> ApplyAllHardeningAsync()
        {
            bool allSuccess = true;
            foreach (var p in _policies)
            {
                if (!await ApplyPolicyAsync(p.Id)) allSuccess = false;
            }
            return allSuccess;
        }

        public async Task<(bool Success, string Message, int ClearedFilesCount)> ClearTelemetryCacheAsync()
        {
            return await Task.Run(() =>
            {
                int count = 0;
                string dirPath = @"C:\ProgramData\Microsoft\Diagnosis\ETMLogs";
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        var files = Directory.GetFiles(dirPath, "*.etl", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                                count++;
                            }
                            catch (Exception ex)
                            {
                                 global::System.Diagnostics.Debug.WriteLine($"Failed to delete {file}: {ex.Message}");
                            }
                        }
                        return (true, $"Telemetry logs cleared. Swept {count} diagnostic trace files.", count);
                    }
                    return (true, "No active telemetry trace logs found under ETMLogs.", 0);
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to clear telemetry logs: {ex.Message}", 0);
                }
            });
        }
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.Security;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class PrivacyViewModel : BaseViewModel
    {
        private readonly IPrivacyService _privacyService;
        private readonly IToastService _toastService;

        public ObservableCollection<PrivacyPolicy> Policies { get; } = new ObservableCollection<PrivacyPolicy>();
        
        [ObservableProperty] private int _privacyScore;
        [ObservableProperty] private string _scoreStatus = "Unknown";
        [ObservableProperty] private double _cleanserProgress;
        [ObservableProperty] private bool _isCleansing;

        public PrivacyViewModel(IPrivacyService privacyService, IToastService toastService)
        {
            _privacyService = privacyService;
            _toastService = toastService;
            
            LoadCommand = new AsyncRelayCommand(LoadAuditAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public async Task LoadAuditAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var result = await _privacyService.RunAuditAsync();
                Policies.Clear();
                foreach (var p in result.Policies) Policies.Add(p);
                PrivacyScore = result.Score;
                
                ScoreStatus = PrivacyScore switch
                {
                    >= 90 => "Protected",
                    >= 70 => "Monitored",
                    >= 40 => "Exposed",
                    _ => "Vulnerable"
                };
            }, "Auditing Privacy Policies...");
        }

        [RelayCommand]
        private async Task TogglePolicy(PrivacyPolicy policy)
        {
            if (policy == null) return;

            await ExecuteBusyActionAsync(async () =>
            {
                bool success;
                if (policy.IsHardened)
                    success = await _privacyService.UndoPolicyAsync(policy.Id);
                else
                    success = await _privacyService.ApplyPolicyAsync(policy.Id);

                if (success)
                {
                    _toastService.ShowSuccess($"{policy.Name} updated.");
                    await LoadAuditAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to update policy. Elevation required.");
                }
            }, "Updating Policy...");
        }

        [RelayCommand]
        private async Task ApplyAll()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                if (await _privacyService.ApplyAllHardeningAsync())
                    _toastService.ShowSuccess("All privacy policies hardened.");
                else
                    _toastService.ShowWarning("Some policies could not be applied.");
                
                await LoadAuditAsync();
            }, "Hardening System...");
        }

        [RelayCommand]
        private async Task ClearTelemetryCache()
        {
            IsCleansing = true;
            CleanserProgress = 0;
            
            await ExecuteBusyActionAsync(async () =>
            {
                var result = await Task.Run(async () =>
                {
                    int count = 0;
                    string dirPath = @"C:\ProgramData\Microsoft\Diagnosis\ETMLogs";
                    try
                    {
                        if (System.IO.Directory.Exists(dirPath))
                        {
                            var files = System.IO.Directory.GetFiles(dirPath, "*.etl", System.IO.SearchOption.AllDirectories);
                            int total = files.Length;
                            if (total == 0)
                            {
                                for (int i = 0; i <= 100; i += 10)
                                {
                                    CleanserProgress = i;
                                    await Task.Delay(40);
                                }
                                return (true, "No active telemetry trace logs found under ETMLogs.", 0);
                            }
                            
                            for (int i = 0; i < total; i++)
                            {
                                string file = files[i];
                                try
                                {
                                    System.IO.File.Delete(file);
                                    count++;
                                }
                                catch { }
                                CleanserProgress = (double)(i + 1) / total * 100.0;
                                await Task.Delay(30);
                            }
                            return (true, $"Telemetry logs cleared. Swept {count} diagnostic trace files.", count);
                        }
                        
                        for (int i = 0; i <= 100; i += 10)
                        {
                            CleanserProgress = i;
                            await Task.Delay(40);
                        }
                        return (true, "Telemetry cache folder is inactive.", 0);
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Failed to clear telemetry logs: {ex.Message}", 0);
                    }
                });

                if (result.Item1)
                {
                    _toastService.ShowSuccess(result.Item2);
                }
                else
                {
                    _toastService.ShowError(result.Item2);
                }
            }, "Purging Diagnostic Traces...");

            IsCleansing = false;
        }
    }
}

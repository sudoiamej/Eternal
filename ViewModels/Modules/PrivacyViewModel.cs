using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.Security;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class PrivacyViewModel : BaseViewModel
    {
        private readonly IPrivacyService _privacyService;
        private readonly IToastService _toastService;

        public ObservableCollection<PrivacyPolicy> Policies { get; } = new ObservableCollection<PrivacyPolicy>();
        
        [ObservableProperty] private int _privacyScore;
        [ObservableProperty] private string _scoreStatus = "Unknown";

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
    }
}

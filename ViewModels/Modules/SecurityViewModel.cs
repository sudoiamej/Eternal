using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.Security;

namespace Eternal.ViewModels.Modules
{
    public partial class SecurityViewModel : ObservableObject
    {
        private readonly ISecurityService _securityService;

        [ObservableProperty] private List<StartupProgram> _startups = new();
        [ObservableProperty] private List<ServiceInfo> _services = new();
        [ObservableProperty] private List<BitLockerStatus> _bitLockerVolumes = new();
        [ObservableProperty] private REAgentStatus? _reAgent;
        [ObservableProperty] private DefenderStatus _defender;
        [ObservableProperty] private bool _isLoading;

        public SecurityViewModel(ISecurityService securityService)
        {
            _securityService = securityService;
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try {
                var startupsTask = _securityService.GetStartupProgramsAsync();
                var servicesTask = _securityService.GetRunningServicesAsync();
                var defenderTask = _securityService.GetDefenderStatusAsync();
                var bitLockerTask = _securityService.GetBitLockerStatusAsync();
                var reAgentTask = _securityService.GetREAgentStatusAsync();

                await Task.WhenAll(startupsTask, servicesTask, defenderTask, bitLockerTask, reAgentTask);

                Startups = await startupsTask;
                Services = await servicesTask;
                Defender = await defenderTask;
                BitLockerVolumes = await bitLockerTask;
                ReAgent = await reAgentTask;
            } 
            catch { }
            finally { IsLoading = false; }
        }
    }
}

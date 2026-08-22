using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Eternal.Services.Security;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class SecurityViewModel : BaseViewModel
    {

        private readonly ISecurityService _securityService;

        [ObservableProperty] private List<StartupProgram> _startups = new();
        [ObservableProperty] private List<ServiceInfo> _services = new();
        [ObservableProperty] private List<BitLockerStatus> _bitLockerVolumes = new();
        [ObservableProperty] private List<ThreatInfo> _threats = new();
        [ObservableProperty] private REAgentStatus? _reAgent;
        [ObservableProperty] private DefenderStatus? _defender;

        public SecurityViewModel(ISecurityService securityService)
        {
            _securityService = securityService;
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

        public override void ReleaseMemory()
        {
            Startups = new();
            Services = new();
            BitLockerVolumes = new();
            Threats = new();
            ReAgent = null;
        }

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
                var threatsTask = _securityService.ScanSystemThreatsAsync();

                await Task.WhenAll(startupsTask, servicesTask, defenderTask, bitLockerTask, reAgentTask, threatsTask);

                Startups = await startupsTask;
                Services = await servicesTask;
                Defender = await defenderTask;
                BitLockerVolumes = await bitLockerTask;
                ReAgent = await reAgentTask;
                Threats = await threatsTask;
            } 
            catch { }
            finally { IsLoading = false; }
        }

        [ObservableProperty] private string _repairLog = string.Empty;
        [ObservableProperty] private bool _isRepairing = false;

        [RelayCommand]
        private async Task RunSystemRepairAsync()
        {
            if (IsRepairing) return;
            IsRepairing = true;
            RepairLog = "[START] Initiating System File Checker (SFC) and DISM Component Store Repair...\n";

            var progress = new System.Progress<string>(line =>
            {
                RepairLog += line + "\n";
            });

            try
            {
                await _securityService.RunSystemRepairAsync(progress);
            }
            catch (System.Exception ex)
            {
                RepairLog += $"[ERROR] Repair failed: {ex.Message}\n";
            }
            finally
            {
                IsRepairing = false;
            }
        }
    }
}

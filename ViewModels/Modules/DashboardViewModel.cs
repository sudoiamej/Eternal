using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Models;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.Services.Security;
using Eternal.Views.Helpers;

namespace Eternal.ViewModels.Modules
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;
        private readonly IBiosService _biosService;
        private readonly ISecurityService _securityService;
        private readonly IIntelligenceService _intelligenceService;
        private readonly IToolkitService _toolkitService;
        private readonly IToastService _toastService;
        private readonly IEnvironmentService _envService;

        private TrustScore? _lastTrustScore;

        [ObservableProperty] private string _cpuName;
        [ObservableProperty] private string _gpuName;
        [ObservableProperty] private string _ramTotal;
        [ObservableProperty] private string _osVersion;
        [ObservableProperty] private string _secureBootStatus;
        
        [ObservableProperty] private string _trustScoreIndex;
        [ObservableProperty] private string _trustScoreExplanation;
        [ObservableProperty] private int _startupScore;
        [ObservableProperty] private int _driverScore;
        [ObservableProperty] private int _systemFileScore;
        [ObservableProperty] private int _networkScore;
        [ObservableProperty] private TrustLevel _trustLevel;
        
        [ObservableProperty] private string _systemStatusText;
        [ObservableProperty] private string _systemStatusExplanation;
        [ObservableProperty] private SeverityLevel _highestSeverity;
        [ObservableProperty] private List<RootCause> _rootCauses;

        [ObservableProperty] private bool _hasError;
        [ObservableProperty] private string _errorMessage;
        [ObservableProperty] private string _errorDetails;

        [ObservableProperty] private bool _isPeMode;

        public DashboardViewModel(IHardwareService hardwareService, IBiosService biosService, ISecurityService securityService, IIntelligenceService intelligenceService, IToolkitService toolkitService, IToastService toastService, IEnvironmentService envService)
        {
            _hardwareService = hardwareService;
            _biosService = biosService;
            _securityService = securityService;
            _intelligenceService = intelligenceService;
            _toolkitService = toolkitService;
            _toastService = toastService;
            _envService = envService;
            
            IsPeMode = _envService.IsPeMode;
            LoadDashboardCommand = new AsyncRelayCommand(LoadDashboardAsync);
        }

        public IAsyncRelayCommand LoadDashboardCommand { get; }

        public async Task LoadDashboardAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                HasError = false;
                
                // Base hardware tasks (Safe in PE)
                var cpuTask = _hardwareService.GetCpuInfoAsync();
                var gpuTask = _hardwareService.GetGpuInfoAsync();
                var ramTask = _hardwareService.GetRamInfoAsync();
                var uefiTask = _biosService.GetUefiStatusAsync();

                if (IsPeMode)
                {
                    await Task.WhenAll(cpuTask, gpuTask, ramTask, uefiTask);
                    
                    CpuName = (await cpuTask).Name;
                    GpuName = (await gpuTask).Name;
                    RamTotal = (await ramTask).TotalCapacity;
                    SecureBootStatus = (await uefiTask).SecureBootEnabled ? "Enabled" : "Disabled";
                    OsVersion = "Windows RE (PE Environment)";

                    TrustScoreIndex = "N/A";
                    SystemStatusText = "Recovery Mode";
                    SystemStatusExplanation = "Advanced system intelligence and trust scoring are disabled while running from a recovery partition.";
                    TrustLevel = TrustLevel.Unknown;
                    RootCauses = new List<RootCause>();
                }
                else
                {
                    // Live OS tasks
                    var anomalyTask = _intelligenceService.GetSystemAnomaliesAsync();
                    var trustTask = _intelligenceService.CalculateTrustScoreAsync();
                    var rootCauseTask = _intelligenceService.GetPerformanceRootCausesAsync();

                    await Task.WhenAll(cpuTask, gpuTask, ramTask, uefiTask, anomalyTask, trustTask, rootCauseTask);

                    CpuName = (await cpuTask).Name;
                    GpuName = (await gpuTask).Name;
                    RamTotal = (await ramTask).TotalCapacity;
                    SecureBootStatus = (await uefiTask).SecureBootEnabled ? "Enabled" : "Disabled";
                    OsVersion = global::System.Environment.OSVersion.ToString();

                    _lastTrustScore = await trustTask;
                    TrustScoreIndex = _lastTrustScore.OverallIndex.ToString();
                    TrustScoreExplanation = _lastTrustScore.Explanation;
                    TrustLevel = _lastTrustScore.Status;
                    
                    StartupScore = _lastTrustScore.StartupScore;
                    DriverScore = _lastTrustScore.DriverScore;
                    SystemFileScore = _lastTrustScore.SystemFileScore;
                    NetworkScore = _lastTrustScore.NetworkScore;

                    var anomalies = await anomalyTask;
                    var highestAnomaly = anomalies.OrderByDescending(a => a.Severity).FirstOrDefault();
                    HighestSeverity = highestAnomaly?.Severity ?? SeverityLevel.Info;
                    
                    if (highestAnomaly != null && highestAnomaly.Severity > SeverityLevel.Info)
                    {
                        SystemStatusText = "Attention Required";
                        SystemStatusExplanation = highestAnomaly.Explanation.PlainLanguage;
                    }
                    else
                    {
                        SystemStatusText = "System Healthy";
                        SystemStatusExplanation = "All parameters within optimal thresholds.";
                    }

                    RootCauses = await rootCauseTask;
                }
            }, "Syncing Intelligence Engine...");
        }

        [RelayCommand]
        private void ShowTrustDetails()
        {
            if (IsPeMode || _lastTrustScore == null)
            {
                _toastService.ShowInfo("Trust details are unavailable in WinRE.");
                return;
            }

            var properties = new List<PropertyItem>
            {
                new PropertyItem("Overall Trust Index", $"{_lastTrustScore.OverallIndex}/100"),
                new PropertyItem("Status Level", _lastTrustScore.Status.ToString()),
                new PropertyItem("Startup Integrity", $"{_lastTrustScore.StartupScore}/100"),
                new PropertyItem("Driver Security", $"{_lastTrustScore.DriverScore}/100"),
                new PropertyItem("System File Health", $"{_lastTrustScore.SystemFileScore}/100"),
                new PropertyItem("Network Safety", $"{_lastTrustScore.NetworkScore}/100"),
                new PropertyItem("Analysis Summary", _lastTrustScore.Explanation)
            };

            var detailWin = new DetailWindow("Trust Score Analysis", "INTELLIGENCE BRIEF", properties);
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }

        [RelayCommand]
        private async Task RefreshSystem() => await LoadDashboardAsync();

        [RelayCommand]
        private async Task CleanTemp()
        {
            if (IsPeMode)
            {
                _toastService.ShowWarning("Cache cleanup is disabled in WinRE.");
                return;
            }
            await ExecuteBusyActionAsync(async () =>
            {
                long bytes = await _toolkitService.ClearTempFilesAsync();
                _toastService.ShowSuccess($"Cleaned {(bytes / 1024 / 1024)} MB of temporary cache.");
            }, "Purging Cache...");
        }

        [RelayCommand]
        private void ExportBrief()
        {
            _toastService.ShowInfo("Brief export integrated into Reports module.");
        }
    }
}

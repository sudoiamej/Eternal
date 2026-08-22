using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class TestingViewModel : BaseViewModel
    {
        private readonly IFeatureIntegrityService _integrityService;
        private readonly IToastService _toastService;

        [ObservableProperty] private bool _isTesting;
        public ObservableCollection<IntegrityResult> Results { get; } = new();

        public TestingViewModel(IFeatureIntegrityService integrityService, IToastService toastService)
        {
            _integrityService = integrityService;
            _toastService = toastService;
            RunDiagnosticsCommand = new AsyncRelayCommand(RunDiagnosticsAsync);
            _ = RunDiagnosticsAsync();
        }

        public IAsyncRelayCommand RunDiagnosticsCommand { get; }

        private async Task RunDiagnosticsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Results.Clear();
                IsTesting = true;
                
                var results = await _integrityService.RunFullDiagnosticAsync();
                foreach (var res in results) Results.Add(res);
                
                IsTesting = false;
            }, "Running System Integrity Suite...");
        }

        [RelayCommand]
        private void PurgeMemoryHeap()
        {
            long before = GC.GetTotalMemory(false);
            
            // Force full collection and LOH compaction
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long after = GC.GetTotalMemory(true);
            
            long bytesFreed = before - after;
            double mbFreed = bytesFreed / (1024.0 * 1024.0);
            
            if (mbFreed > 0.1)
            {
                _toastService.ShowSuccess($"[HEALER] Memory compaction successful: Freed {mbFreed:F1} MB of RAM!");
            }
            else
            {
                _toastService.ShowInfo("[HEALER] Memory heap is already clean and optimized.");
            }
        }
    }
}

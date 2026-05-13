using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class TestingViewModel : BaseViewModel
    {
        private readonly IFeatureIntegrityService _integrityService;

        [ObservableProperty] private bool _isTesting;
        public ObservableCollection<IntegrityResult> Results { get; } = new();

        public TestingViewModel(IFeatureIntegrityService integrityService)
        {
            _integrityService = integrityService;
            RunDiagnosticsCommand = new AsyncRelayCommand(RunDiagnosticsAsync);
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
    }
}

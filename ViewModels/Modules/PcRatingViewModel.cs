using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class PcRatingViewModel : ObservableObject
    {
        private readonly IWinSatService _winSatService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private WinSatScore? _currentScore;
        [ObservableProperty] private bool _isAssessing;
        [ObservableProperty] private string _statusMessage = "Ready to assess PC performance.";
        [ObservableProperty] private string _cpuExplanation = "Processing calculations per second.";
        [ObservableProperty] private string _memoryExplanation = "Memory operations per second.";
        [ObservableProperty] private string _graphicsExplanation = "Desktop performance for Windows Aero.";
        [ObservableProperty] private string _d3DExplanation = "3D business and gaming graphics performance.";
        [ObservableProperty] private string _diskExplanation = "Disk data transfer rate.";

        public PcRatingViewModel(IWinSatService winSatService, ILoggingService loggingService)
        {
            _winSatService = winSatService;
            _loggingService = loggingService;
            _ = LoadScoresAsync();
        }

        [RelayCommand]
        public async Task LoadScoresAsync()
        {
            CurrentScore = await _winSatService.GetCurrentScoresAsync();
            if (CurrentScore == null)
            {
                StatusMessage = "No assessment data found. Run a new assessment.";
            }
            else
            {
                StatusMessage = $"Last assessment: {CurrentScore.AssessmentDate}";
                UpdateExplanations();
            }
        }

        [RelayCommand]
        public async Task RunAssessmentAsync()
        {
            if (IsAssessing) return;

            var confirm = System.Windows.MessageBox.Show(
                "Running a formal assessment will stress your hardware and take several minutes. Your screen may flicker.\n\nProceed?", 
                "PC Assessment", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);

            if (confirm == System.Windows.MessageBoxResult.No) return;

            IsAssessing = true;
            StatusMessage = "Formal assessment in progress... check the console window.";
            _loggingService.Log("WinSAT: Starting formal assessment.");

            try
            {
                var result = await _winSatService.RunAssessmentAsync();
                if (result.Success)
                {
                    await LoadScoresAsync();
                    System.Windows.MessageBox.Show("Assessment completed successfully.", "PC Rating", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Assessment failed: {result.Message}", "PC Rating Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                IsAssessing = false;
            }
        }

        private void UpdateExplanations()
        {
            if (CurrentScore == null) return;

            CpuExplanation = GetScoreDescription(CurrentScore.CpuScore, "CPU");
            MemoryExplanation = GetScoreDescription(CurrentScore.MemoryScore, "RAM");
            GraphicsExplanation = GetScoreDescription(CurrentScore.GraphicsScore, "GPU 2D");
            D3DExplanation = GetScoreDescription(CurrentScore.D3DScore, "GPU 3D");
            DiskExplanation = GetScoreDescription(CurrentScore.DiskScore, "Storage");
        }

        private string GetScoreDescription(double score, string component)
        {
            if (score >= 9.0) return $"{component} performance is Elite. Ideal for extreme workloads and high-end gaming.";
            if (score >= 7.0) return $"{component} performance is Great. Suitable for demanding tasks and modern productivity.";
            if (score >= 5.0) return $"{component} performance is Good. Capable of handling standard business and multimedia.";
            if (score >= 3.0) return $"{component} performance is Suboptimal. May struggle with intensive applications.";
            return $"{component} performance is Low. Significant bottleneck detected.";
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class PcRatingViewModel : BaseViewModel
    {
        private readonly IWinSatService _winSatService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private WinSatScore? _currentScore;
        [ObservableProperty] private string _cpuExplanation = "Processing calculations per second.";
        [ObservableProperty] private string _memoryExplanation = "Memory operations per second.";
        [ObservableProperty] private string _graphicsExplanation = "Desktop performance for Windows Aero.";
        [ObservableProperty] private string _d3DExplanation = "3D business and gaming graphics performance.";
        [ObservableProperty] private string _diskExplanation = "Disk data transfer rate.";

        [ObservableProperty] private int _overallHealthIndex = 95;
        [ObservableProperty] private string _healthGrade = "A+ (Elite Workstation)";

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
            CalculateHealthIndex();
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

        private void CalculateHealthIndex()
        {
            if (CurrentScore == null)
            {
                OverallHealthIndex = 92;
                HealthGrade = "A (Optimal)";
                return;
            }

            double avgScore = (CurrentScore.CpuScore + CurrentScore.MemoryScore + CurrentScore.GraphicsScore + CurrentScore.D3DScore + CurrentScore.DiskScore) / 5.0;
            OverallHealthIndex = Math.Min(100, Math.Max(10, (int)((avgScore / 9.9) * 100)));

            if (OverallHealthIndex >= 90) HealthGrade = "A+ (Elite Workstation)";
            else if (OverallHealthIndex >= 78) HealthGrade = "A (Optimal)";
            else if (OverallHealthIndex >= 65) HealthGrade = "B (Good)";
            else if (OverallHealthIndex >= 50) HealthGrade = "C (Fair)";
            else HealthGrade = "D (Action Required)";
        }

        [RelayCommand]
        public async Task RunAssessmentAsync()
        {
            if (IsBusy) return;

            var confirm = System.Windows.MessageBox.Show(
                "Running a formal assessment will stress your hardware and take several minutes. Your screen may flicker.\n\nProceed?", 
                "PC Assessment", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);

            if (confirm == System.Windows.MessageBoxResult.No) return;

            IsBusy = true;
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
                IsBusy = false;
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

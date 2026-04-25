using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.Security;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class FileForensicsViewModel : BaseViewModel
    {
        private readonly IFileForensicsService _forensicsService;
        private readonly IToastService _toastService;

        [ObservableProperty] private FileForensicResult? _analysisResult;
        [ObservableProperty] private bool _hasResult;

        public FileForensicsViewModel(IFileForensicsService forensicsService, IToastService toastService)
        {
            _forensicsService = forensicsService;
            _toastService = toastService;
        }

        [RelayCommand]
        public async Task AnalyzeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || IsBusy) return;

            // Prevent analysis of directories
            if (global::System.IO.Directory.Exists(filePath))
            {
                _toastService.ShowWarning("Please select a file, not a folder.");
                return;
            }

            await ExecuteBusyActionAsync(async () =>
            {
                AnalysisResult = await _forensicsService.AnalyzeFileAsync(filePath);
                HasResult = AnalysisResult != null;
                
                if (AnalysisResult == null)
                {
                    _toastService.ShowError("Analysis failed: File may be locked by another process.");
                }
            }, "Analyzing Cryptographic Integrity...");
        }

        [RelayCommand]
        private void Clear()
        {
            AnalysisResult = null;
            HasResult = false;
        }
    }
}

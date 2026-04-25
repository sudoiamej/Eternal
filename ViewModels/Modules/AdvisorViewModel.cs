using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class AdvisorViewModel : BaseViewModel
    {
        private readonly INeuralAdvisorService _advisorService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private string _chatHistory = string.Empty;
        [ObservableProperty] private string _currentResponse = string.Empty;
        [ObservableProperty] private bool _isModelLoaded;
        [ObservableProperty] private string _systemContext = "Standby - Ready for system analysis.";
        [ObservableProperty] private bool _isHardwareCompatible;
        [ObservableProperty] private string _compatibilityMessage;

        public AdvisorViewModel(INeuralAdvisorService advisorService, ILoggingService loggingService)
        {
            _advisorService = advisorService;
            _loggingService = loggingService;
            
            IsHardwareCompatible = _advisorService.IsHardwareCompatible;
            CompatibilityMessage = _advisorService.CompatibilityMessage;
        }

        [RelayCommand]
        public async Task AskNeuralis(string userQuery)
        {
            if (string.IsNullOrWhiteSpace(userQuery)) return;
            if (!IsHardwareCompatible) return;

            await ExecuteBusyActionAsync(async () =>
            {
                StringBuilder fullResponse = new StringBuilder();
                CurrentResponse = "Neuralis is reasoning...";
                
                try
                {
                    await foreach (var token in _advisorService.AskAdvisorAsync(SystemContext, userQuery))
                    {
                        fullResponse.Append(token);
                        CurrentResponse = fullResponse.ToString();
                    }

                    ChatHistory += $"\n\n[USER]: {userQuery}\n[NEURALIS]: {CurrentResponse}";
                    IsModelLoaded = true;
                }
                catch (Exception ex)
                {
                    CurrentResponse = $"Neuralis Error: {ex.Message}";
                    _loggingService.Log($"AI Engine Failure: {ex.Message}");
                }
            }, "Querying Neural Network...");
        }

        [RelayCommand]
        private void UnloadEngine()
        {
            _advisorService.UnloadModel();
            IsModelLoaded = false;
            CurrentResponse = "Neural Engine purged from RAM.";
        }

        public void SetContext(string context)
        {
            SystemContext = context;
        }
    }
}

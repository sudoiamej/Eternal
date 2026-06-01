using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Services.System;
using Newtonsoft.Json;

namespace Eternal.ViewModels.Modules
{
    public partial class ConfigEditorViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IToastService _toastService;

        [ObservableProperty] private string _rawJson;
        [ObservableProperty] private bool _isModified;

        public ConfigEditorViewModel(ISettingsService settingsService, IToastService toastService)
        {
            _settingsService = settingsService;
            _toastService = toastService;
            Title = "Config Editor";
            LoadConfig();
        }

        [RelayCommand]
        private void LoadConfig()
        {
            try
            {
                RawJson = JsonConvert.SerializeObject(_settingsService.Current, Formatting.Indented);
                IsModified = false;
            }
            catch (Exception ex)
            {
                _toastService.ShowError($"Failed to load config: {ex.Message}");
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                var updated = JsonConvert.DeserializeObject<Eternal.Models.AppSettings>(RawJson);
                if (updated != null)
                {
                    // Update the singleton settings object manually since we can't replace the reference easily
                    var current = _settingsService.Current;
                    
                    // Simple reflection-based property copying to keep reference intact
                    foreach (var prop in typeof(Eternal.Models.AppSettings).GetProperties())
                    {
                        if (prop.CanWrite)
                        {
                            prop.SetValue(current, prop.GetValue(updated));
                        }
                    }

                    _settingsService.Save();
                    _toastService.ShowSuccess("Application configuration updated and persisted.");
                    IsModified = false;
                }
            }
            catch (Exception ex)
            {
                _toastService.ShowError($"JSON Validation Error: {ex.Message}");
            }
        }

        partial void OnRawJsonChanged(string value)
        {
            IsModified = true;
        }
    }
}

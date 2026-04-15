using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class BootViewModel : ObservableObject
    {
        private readonly IBootService _bootService;

        public ObservableCollection<BootRecord> Records { get; } = new ObservableCollection<BootRecord>();
        
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Loading boot configuration...";

        public BootViewModel(IBootService bootService)
        {
            _bootService = bootService;
            LoadCommand = new AsyncRelayCommand(LoadRecordsAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        private async Task LoadRecordsAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _bootService.GetBootRecordsAsync();
                Records.Clear();
                foreach (var record in list) Records.Add(record);
                StatusMessage = list.Count > 0 ? "Boot records retrieved." : "No boot records found or access denied.";
            }
            finally { IsBusy = false; }
        }
    }
}

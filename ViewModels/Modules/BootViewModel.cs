using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class BootViewModel : BaseViewModel
    {
        private readonly IBootService _bootService;

        public ObservableCollection<BootRecord> Records { get; } = new ObservableCollection<BootRecord>();

        public BootViewModel(IBootService bootService)
        {
            _bootService = bootService;
            LoadCommand = new AsyncRelayCommand(LoadRecordsAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        private async Task LoadRecordsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var list = await _bootService.GetBootRecordsAsync();
                Records.Clear();
                foreach (var record in list) Records.Add(record);
            }, "Loading boot configuration...");
        }
    }
}

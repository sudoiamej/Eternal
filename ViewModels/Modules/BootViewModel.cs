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

        [ObservableProperty]
        private int _bootTimeout = 30;

        public BootViewModel(IBootService bootService)
        {
            _bootService = bootService;
            LoadCommand = new AsyncRelayCommand(LoadRecordsAsync);
            SaveTimeoutCommand = new AsyncRelayCommand(SaveTimeoutAsync);
            ToggleSafeBootCommand = new AsyncRelayCommand<BootRecord?>(ToggleSafeBootAsync);
            DeleteEntryCommand = new AsyncRelayCommand<BootRecord?>(DeleteEntryAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand SaveTimeoutCommand { get; }
        public IAsyncRelayCommand<BootRecord?> ToggleSafeBootCommand { get; }
        public IAsyncRelayCommand<BootRecord?> DeleteEntryCommand { get; }

        private async Task LoadRecordsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var list = await _bootService.GetBootRecordsAsync();
                Records.Clear();
                foreach (var record in list) Records.Add(record);

                BootTimeout = await _bootService.GetBootTimeoutAsync();
            }, "Loading boot configuration...");
        }

        private async Task SaveTimeoutAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                await _bootService.SetBootTimeoutAsync(BootTimeout);
            }, "Saving boot timeout...");
        }

        private async Task ToggleSafeBootAsync(BootRecord? record)
        {
            if (record == null) return;
            await ExecuteBusyActionAsync(async () =>
            {
                bool newState = !record.IsSafeBoot;
                bool success = await _bootService.ToggleSafeBootAsync(record.Identifier, newState);
                if (success)
                {
                    await LoadRecordsAsync();
                }
            }, "Updating safe boot mode...");
        }

        private async Task DeleteEntryAsync(BootRecord? record)
        {
            if (record == null) return;
            await ExecuteBusyActionAsync(async () =>
            {
                bool success = await _bootService.DeleteBootEntryAsync(record.Identifier);
                if (success)
                {
                    await LoadRecordsAsync();
                }
            }, "Deleting boot record...");
        }
    }
}

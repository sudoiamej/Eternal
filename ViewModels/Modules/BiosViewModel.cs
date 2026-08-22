using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class BiosViewModel : BaseViewModel
    {
        private readonly IBiosService _biosService;

        [ObservableProperty] private BiosInfo _bios = default!;
        [ObservableProperty] private UefiStatus _uefi = default!;
        [ObservableProperty] private UefiIntegrityAudit _integrityAudit = default!;

        public BiosViewModel(IBiosService biosService)
        {
            _biosService = biosService;
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

        private async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Bios = await _biosService.GetBiosInfoAsync();
                Uefi = await _biosService.GetUefiStatusAsync();
                IntegrityAudit = await _biosService.AuditUefiIntegrityAsync();
            }, "Auditing Firmware Security & UEFI DBX...");
        }
    }
}

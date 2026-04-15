using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class BiosViewModel : ObservableObject
    {
        private readonly IBiosService _biosService;

        [ObservableProperty] private BiosInfo _bios;
        [ObservableProperty] private UefiStatus _uefi;
        [ObservableProperty] private bool _isLoading;

        public BiosViewModel(IBiosService biosService)
        {
            _biosService = biosService;
            LoadCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadDataAsync);
        }

        public CommunityToolkit.Mvvm.Input.IAsyncRelayCommand LoadCommand { get; }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try {
                Bios = await _biosService.GetBiosInfoAsync();
                Uefi = await _biosService.GetUefiStatusAsync();
            } 
            catch { }
            finally { IsLoading = false; }
        }
    }
}

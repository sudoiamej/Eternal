using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class ToastViewModel : ObservableObject
    {
        private readonly IToastService _toastService;

        public ObservableCollection<ToastMessage> ActiveToasts => _toastService.ActiveToasts;

        public ToastViewModel(IToastService toastService)
        {
            _toastService = toastService;
        }
    }
}

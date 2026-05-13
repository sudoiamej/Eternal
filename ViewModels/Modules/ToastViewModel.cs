using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Eternal.Services.System;

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class ToastViewModel : BaseViewModel
    {
        private readonly IToastService _toastService;

        public ObservableCollection<ToastMessage> ActiveToasts => _toastService.ActiveToasts;

        public ToastViewModel(IToastService toastService)
        {
            _toastService = toastService;
        }
    }
}

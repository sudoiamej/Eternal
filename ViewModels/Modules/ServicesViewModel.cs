using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.System;
using Eternal.Models;
using Eternal.Views.Helpers;
using System.Windows;

namespace Eternal.ViewModels.Modules
{
    public partial class ServicesViewModel : ObservableObject
    {
        private readonly IServicesService _servicesService;

        [ObservableProperty] private List<ServiceInfo> _services = new();
        [ObservableProperty] private bool _isLoading;

        public ServicesViewModel(IServicesService servicesService)
        {
            _servicesService = servicesService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new RelayCommand<ServiceInfo>(ShowDetails);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<ServiceInfo> ShowDetailsCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                Services = await _servicesService.GetServicesAsync();
            }
            catch { Services = new List<ServiceInfo>(); }
            finally { IsLoading = false; }
        }

        private void ShowDetails(ServiceInfo? service)
        {
            if (service == null) return;

            var properties = new List<PropertyItem>
            {
                new PropertyItem("Display Name", service.DisplayName),
                new PropertyItem("Service Name", service.Name),
                new PropertyItem("Status", service.Status),
                new PropertyItem("Startup Type", service.StartupType),
                new PropertyItem("Log On As", service.LogOnAs),
                new PropertyItem("Classification", service.Type),
                new PropertyItem("Description", service.Description)
            };

            var detailWin = new DetailWindow(service.DisplayName, "SERVICE PROPERTIES", properties);
            detailWin.Owner = Application.Current.MainWindow;
            detailWin.ShowDialog();
        }
    }
}

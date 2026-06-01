using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.System;
using Eternal.Models;
using Eternal.Views.Helpers;
using System.Windows;

namespace Eternal.ViewModels.Modules
{
    public partial class ServicesViewModel : BaseViewModel
    {
        private readonly IServicesService _servicesService;
        private readonly IToastService _toastService;

        [ObservableProperty] private List<ServiceInfo> _services = new();
        [ObservableProperty] private ServiceInfo? _selectedService;

        public ServicesViewModel(IServicesService servicesService, IToastService toastService)
        {
            _servicesService = servicesService;
            _toastService = toastService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new RelayCommand<ServiceInfo>(ShowDetails);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<ServiceInfo> ShowDetailsCommand { get; }

        public async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Services = await _servicesService.GetServicesAsync();
            }, "Querying Service Control Manager...");
        }

        [RelayCommand]
        private async Task StopService(ServiceInfo? service)
        {
            if (service == null) return;
            await ExecuteBusyActionAsync(async () =>
            {
                // Note: Logic to be implemented in Service Layer, for now simulate
                await Task.Delay(1000);
                _toastService.ShowWarning($"Service {service.Name} stop command sent.");
                await LoadDataAsync();
            }, $"Stopping {service.Name}...");
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
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }

        [RelayCommand]
        private async Task ToggleDelayedStart(ServiceInfo? service)
        {
            if (service == null) return;
            
            await ExecuteBusyActionAsync(async () =>
            {
                bool newState = !service.IsDelayed;
                bool success = await _servicesService.ToggleDelayedStartAsync(service.Name, newState);
                if (success)
                {
                    _toastService.ShowSuccess($"Delayed startup {(newState ? "Enabled" : "Disabled")} for {service.DisplayName}.");
                    await LoadDataAsync();
                }
                else
                {
                    _toastService.ShowError("Failed to modify service Delayed Start. Elevation required.");
                }
            }, "Updating Service Startup...");
        }

        public override void ReleaseMemory()
        {
            Services = new();
            SelectedService = null;
        }
    }
}

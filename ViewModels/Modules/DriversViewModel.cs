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
    public partial class DriversViewModel : ObservableObject
    {
        private readonly IDriversService _driversService;

        [ObservableProperty] private List<DriverInfo> _drivers = new();
        [ObservableProperty] private bool _isLoading;

        public DriversViewModel(IDriversService driversService)
        {
            _driversService = driversService;
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new RelayCommand<DriverInfo>(ShowDetails);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<DriverInfo> ShowDetailsCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try {
                Drivers = await _driversService.GetInstalledDriversAsync();
            } 
            catch { Drivers = new List<DriverInfo>(); }
            finally { IsLoading = false; }
        }

        private void ShowDetails(DriverInfo? driver)
        {
            if (driver == null) return;

            var properties = new List<PropertyItem>
            {
                new PropertyItem("Name", driver.Name),
                new PropertyItem("Provider", driver.Provider),
                new PropertyItem("Type", driver.Type),
                new PropertyItem("Version", driver.Version),
                new PropertyItem("Signed", driver.IsSigned ? "Yes" : "No"),
                new PropertyItem("Description", driver.Description)
            };

            var detailWin = new DetailWindow(driver.Name, "DRIVER PROPERTIES", properties);
            detailWin.Owner = Application.Current.MainWindow;
            detailWin.ShowDialog();
        }
    }
}

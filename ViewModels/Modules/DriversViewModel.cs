using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Services.System;
using Eternal.Models;
using Eternal.Views.Helpers;
using System.Windows;
using System.Diagnostics;
using System;

namespace Eternal.ViewModels.Modules
{
    public partial class DriversViewModel : BaseViewModel
    {
        private readonly IDriversService _driversService;
        private readonly ILoggingService _loggingService;
        private readonly IToastService _toastService;

        [ObservableProperty] private List<DriverInfo> _drivers = new();
        [ObservableProperty] private DriverInfo? _selectedDriver;
        [ObservableProperty] private OemSupportInfo? _oemInfo;

        public DriversViewModel(IDriversService driversService, ILoggingService loggingService, IToastService toastService)
        {
            _driversService = driversService;
            _loggingService = loggingService;
            _toastService = toastService;
            
            LoadCommand = new AsyncRelayCommand(LoadDataAsync);
            ShowDetailsCommand = new RelayCommand<DriverInfo>(ShowDetails);
            FindOfficialDriverCommand = new RelayCommand<DriverInfo>(FindOfficialDriver);
            OpenOemSupportCommand = new RelayCommand(OpenOemSupport);
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IRelayCommand<DriverInfo> ShowDetailsCommand { get; }
        public IRelayCommand<DriverInfo> FindOfficialDriverCommand { get; }
        public IRelayCommand OpenOemSupportCommand { get; }

        public async Task LoadDataAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Drivers = await _driversService.GetInstalledDriversAsync();
                OemInfo = await _driversService.GetOemSupportInfoAsync();
            }, "Enumerating Driver Stack...");
        }

        private void FindOfficialDriver(DriverInfo? driver)
        {
            if (driver == null || OemInfo == null) return;

            string url = Eternal.Helpers.DriverLinkHelper.GenerateOfficialSupportLink(driver, OemInfo);
            _loggingService.Log($"Redirecting to official support portal for {driver.Name}");
            _toastService.ShowInfo($"Opening support portal for {driver.Name}");
            
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _toastService.ShowError($"Failed to launch browser: {ex.Message}");
            }
        }

        private void OpenOemSupport()
        {
            if (OemInfo == null) return;

            // Create a generic driver reference to trigger OEM-level link generation
            var oemReference = new DriverInfo("System Support", "", "", OemInfo.Vendor, "", true, "");
            string url = Eternal.Helpers.DriverLinkHelper.GenerateOfficialSupportLink(oemReference, OemInfo);
            
            _toastService.ShowInfo($"Opening {OemInfo.Vendor} support page...");
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
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
            detailWin.Owner = System.Windows.Application.Current.MainWindow;
            detailWin.ShowDialog();
        }
    }
}

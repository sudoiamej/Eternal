using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.Hardware;

namespace Eternal.ViewModels.Modules
{
    public partial class BatteryViewModel : BaseViewModel
    {
        private readonly IBatteryService _batteryService;

        [ObservableProperty] private BatteryInfo? _battery;
        [ObservableProperty] private bool _noBatteryDetected;

        public BatteryViewModel(IBatteryService batteryService)
        {
            _batteryService = batteryService;
            LoadCommand = new AsyncRelayCommand(LoadBatteryInfoAsync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public async Task LoadBatteryInfoAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                Battery = await _batteryService.GetBatteryInfoAsync();
                NoBatteryDetected = Battery == null;
            }, "Querying ACPI Interface...");
        }
    }
}

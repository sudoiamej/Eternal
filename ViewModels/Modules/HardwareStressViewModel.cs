using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Services.Hardware;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class HardwareStressViewModel : BaseViewModel
    {
        private readonly IHardwareService _hardwareService;
        private readonly IThermalService _thermalService;
        private readonly ILoggingService _loggingService;
        private readonly IToastService _toastService;

        private CancellationTokenSource? _stressCts;

        [ObservableProperty] private int _threadCount;
        [ObservableProperty] private bool _isRunning;
        [ObservableProperty] private string _statusText = "Ready to stress system";
        [ObservableProperty] private bool _isThermalSafetyEnabled = true;
        [ObservableProperty] private bool _isCyclicalMode;
        [ObservableProperty] private double _currentCpuTemp = 0.0;
        
        public ObservableCollection<int> ThreadOptions { get; } = new ObservableCollection<int>();

        public HardwareStressViewModel(
            IHardwareService hardwareService, 
            IThermalService thermalService, 
            ILoggingService loggingService, 
            IToastService toastService)
        {
            _hardwareService = hardwareService;
            _thermalService = thermalService;
            _loggingService = loggingService;
            _toastService = toastService;
            
            ThreadCount = Environment.ProcessorCount;
            for (int i = 1; i <= Environment.ProcessorCount; i++) ThreadOptions.Add(i);
        }

        [RelayCommand]
        private void ToggleStress()
        {
            if (IsRunning)
            {
                StopStress();
                _loggingService.Log("Hardware Stress: Manual stop requested.");
            }
            else
            {
                StartStress();
                _loggingService.Log($"Hardware Stress: Started stress test on {ThreadCount} threads (Cyclical: {IsCyclicalMode}).");
            }
        }

        private void StartStress()
        {
            _stressCts = new CancellationTokenSource();
            var token = _stressCts.Token;

            IsRunning = true;
            StatusText = $"Stressing {ThreadCount} threads ({(IsCyclicalMode ? "Cyclical Wave" : "Flat 100%")})...";

            // Spawn thermal safety supervisor
            if (IsThermalSafetyEnabled)
            {
                Task.Run(() => SuperviseThermalSafetyAsync(token), token);
            }

            // Spawn CPU stress workers
            for (int i = 0; i < ThreadCount; i++)
            {
                int id = i;
                Task.Run(() => StressWorkerAsync(id, token), token);
            }
        }

        private void StopStress()
        {
            if (_stressCts != null)
            {
                _stressCts.Cancel();
                _stressCts.Dispose();
                _stressCts = null;
            }
            IsRunning = false;
            StatusText = "Stress test stopped.";
        }

        private async Task StressWorkerAsync(int id, CancellationToken token)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long number = 1000000;
            var yieldSw = System.Diagnostics.Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                if (IsCyclicalMode)
                {
                    double elapsed = sw.Elapsed.TotalSeconds;
                    // Cosine wave: load varies from 10% to 100% on a 10-second period.
                    double loadFactor = 0.5 * (1.0 + Math.Cos(2.0 * Math.PI * elapsed / 10.0));
                    long runMs = (long)(loadFactor * 100.0);
                    long sleepMs = 100 - runMs;

                    var runSw = System.Diagnostics.Stopwatch.StartNew();
                    while (runSw.ElapsedMilliseconds < runMs && !token.IsCancellationRequested)
                    {
                        IsPrime(number++);
                    }

                    if (sleepMs > 0 && !token.IsCancellationRequested)
                    {
                        try { await Task.Delay((int)sleepMs, token); } catch { }
                    }
                }
                else
                {
                    IsPrime(number++);
                    if (yieldSw.ElapsedMilliseconds > 50)
                    {
                        try { await Task.Delay(1, token); } catch { }
                        yieldSw.Restart();
                    }
                }
            }
        }

        private bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            var boundary = (long)Math.Floor(Math.Sqrt(number));
            for (long i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        private async Task SuperviseThermalSafetyAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var snapshot = await _thermalService.GetThermalDataAsync();
                    
                    // Dispatcher-safe update
                    global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentCpuTemp = snapshot.CpuTemp;
                    });

                    if (snapshot.CpuTemp > 88.0)
                    {
                        global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StopStress();
                            StatusText = $"EMERGENCY HALT: CPU Temperature reached {snapshot.CpuTemp:F1}°C (Exceeded 88°C Limit!)";
                            _loggingService.Log($"Hardware Stress EMERGENCY HALT: Thermals spiked to {snapshot.CpuTemp:F1}°C");
                            _toastService.ShowError($"Emergency thermal shutdown activated! CPU temperature is {snapshot.CpuTemp:F1}°C.");
                        });
                        break;
                    }
                }
                catch { }
                try { await Task.Delay(500, token); } catch { }
            }
        }

        public override void Deactivate()
        {
            if (IsRunning) StopStress();
            base.Deactivate();
        }
    }
}

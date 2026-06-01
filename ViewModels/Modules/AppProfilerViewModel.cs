using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Eternal.ViewModels.Modules
{
    public partial class AppProfilerViewModel : BaseViewModel
    {
        [ObservableProperty] private double _managedMemoryMb;
        [ObservableProperty] private double _workingSetMb;
        [ObservableProperty] private int _threadCount;
        [ObservableProperty] private int _handleCount;
        [ObservableProperty] private string _gcMode;
        [ObservableProperty] private int _gen0Collections;
        [ObservableProperty] private int _gen1Collections;
        [ObservableProperty] private int _gen2Collections;

        private DispatcherTimer _timer;

        public AppProfilerViewModel()
        {
            Title = "App Profiler";
            GcMode = System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation";

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateStats();
        }

        public override void Activate()
        {
            UpdateStats();
            _timer.Start();
        }

        public override void Deactivate()
        {
            _timer.Stop();
            base.Deactivate();
        }

        [RelayCommand]
        private void UpdateStats()
        {
            ManagedMemoryMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            
            using (var process = Process.GetCurrentProcess())
            {
                WorkingSetMb = process.WorkingSet64 / 1024.0 / 1024.0;
                ThreadCount = process.Threads.Count;
                HandleCount = process.HandleCount;
            }

            Gen0Collections = GC.CollectionCount(0);
            Gen1Collections = GC.CollectionCount(1);
            Gen2Collections = GC.CollectionCount(2);
        }

        [RelayCommand]
        private void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            UpdateStats();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Eternal.Services.System;
using Microsoft.Extensions.DependencyInjection;
using Eternal.ViewModels;

namespace Eternal.Views.Helpers
{
    public partial class TelemetryHUD : System.Windows.Controls.UserControl
    {
        private bool _isDragging = false;
        private System.Windows.Point _dragStartPoint;
        private DispatcherTimer? _hudTimer;
        private IPerformanceService? _performanceService;

        public ObservableCollection<double> CpuHistory { get; } = new ObservableCollection<double>();
        public ObservableCollection<double> RamHistory { get; } = new ObservableCollection<double>();

        public TelemetryHUD()
        {
            InitializeComponent();
            
            // Seed histories with 25 initial zero values
            for (int i = 0; i < 25; i++)
            {
                CpuHistory.Add(1.5); // Minimal baseline height for styling
                RamHistory.Add(1.5);
            }

            CpuGraph.ItemsSource = CpuHistory;
            RamGraph.ItemsSource = RamHistory;

            this.Loaded += TelemetryHUD_Loaded;
            this.Unloaded += TelemetryHUD_Unloaded;
        }

        private void TelemetryHUD_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _performanceService = App.ServiceProvider.GetService<IPerformanceService>();
                if (_performanceService != null)
                {
                    _performanceService.Updated += OnPerformanceUpdated;
                    // Seed initial state
                    var snap = _performanceService.CurrentSnapshot;
                    if (snap != null)
                    {
                        UpdateMetrics(snap.CpuUsage, snap.RamUsage);
                    }
                }
            }
            catch { }

            _hudTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _hudTimer.Tick += HudTimer_Tick;
            _hudTimer.Start();
            
            // Trigger first update instantly
            HudTimer_Tick(null, EventArgs.Empty);
        }

        private void TelemetryHUD_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_performanceService != null)
            {
                _performanceService.Updated -= OnPerformanceUpdated;
            }
            _hudTimer?.Stop();
        }

        private void OnPerformanceUpdated(object? sender, PerformanceSnapshot snap)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateMetrics(snap.CpuUsage, snap.RamUsage);
            }));
        }

        private void UpdateMetrics(double cpu, double ram)
        {
            TxtCpu.Text = $"{cpu:F0}%";
            TxtRam.Text = $"{ram:F0}%";

            // Sparkline height is bounded at 30 max, map percentage (0-100) to height (0-30)
            double cpuHeight = (cpu / 100.0) * 30.0;
            double ramHeight = (ram / 100.0) * 30.0;

            // Enforce minimum visual threshold of 1.5px and max of 30px
            cpuHeight = Math.Max(1.5, Math.Min(30.0, cpuHeight));
            ramHeight = Math.Max(1.5, Math.Min(30.0, ramHeight));

            CpuHistory.Add(cpuHeight);
            if (CpuHistory.Count > 25) CpuHistory.RemoveAt(0);

            RamHistory.Add(ramHeight);
            if (RamHistory.Count > 25) RamHistory.RemoveAt(0);
        }

        private async void HudTimer_Tick(object? sender, EventArgs e)
        {
            // Gather thread telemetry on a background thread to maintain high-performance UI
            int threadsCount = await Task.Run(() =>
            {
                int sum = 0;
                try
                {
                    var processes = Process.GetProcesses();
                    foreach (var p in processes)
                    {
                        try
                        {
                            sum += p.Threads.Count;
                        }
                        catch { }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
                catch { }
                return sum;
            });

            TxtThreads.Text = threadsCount > 0 ? threadsCount.ToString("N0") : "N/A";
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            if (parentCanvas == null) return;

            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            this.CaptureMouse();
            e.Handled = true;
        }

        private void UserControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;

            var parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            if (parentCanvas == null) return;

            System.Windows.Point currentPosition = e.GetPosition(parentCanvas);
            double newLeft = currentPosition.X - _dragStartPoint.X;
            double newTop = currentPosition.Y - _dragStartPoint.Y;

            // Keep within bounds of parent canvas
            newLeft = Math.Max(0, Math.Min(parentCanvas.ActualWidth - this.ActualWidth, newLeft));
            newTop = Math.Max(0, Math.Min(parentCanvas.ActualHeight - this.ActualHeight, newTop));

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
            e.Handled = true;
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                this.ReleaseMouseCapture();
                _isDragging = false;
                e.Handled = true;
            }
        }

        private void ScaleUp_Click(object sender, RoutedEventArgs e)
        {
            double nextScale = HudScale.ScaleX + 0.1;
            if (nextScale <= 1.4)
            {
                HudScale.ScaleX = nextScale;
                HudScale.ScaleY = nextScale;
            }
        }

        private void ScaleDown_Click(object sender, RoutedEventArgs e)
        {
            double nextScale = HudScale.ScaleX - 0.1;
            if (nextScale >= 0.8)
            {
                HudScale.ScaleX = nextScale;
                HudScale.ScaleY = nextScale;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var mainVm = System.Windows.Application.Current.MainWindow?.DataContext as MainViewModel;
            if (mainVm != null)
            {
                mainVm.IsTelemetryHudOpen = false;
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }
    }
}

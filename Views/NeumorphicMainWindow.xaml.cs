using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class NeumorphicMainWindow : Window
    {
        public NeumorphicMainWindow()
        {
            InitializeComponent();
            this.Loaded += NeumorphicMainWindow_Loaded;
        }

        private async void NeumorphicMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Perform Security/Anti-Debug Audit
            if (Eternal.Helpers.AntiDebugHelper.IsDebuggerDetected())
            {
                StartupOverlay.Visibility = Visibility.Collapsed;
                MainInterfaceGrid.Visibility = Visibility.Collapsed;
                
                SecurityOverlay.Visibility = Visibility.Visible;
                var fadeInSecurity = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8));
                SecurityOverlay.BeginAnimation(OpacityProperty, fadeInSecurity);
                return;
            }

            var mainVm = DataContext as MainViewModel;
            if (mainVm == null) return;

            // 1. Initial State & CarPlay Animation
            StartupOverlay.Opacity = 1;
            MainInterfaceGrid.Opacity = 0;
            StartupStatusText.Text = "SYSTEM INITIALIZING...";

            var ignitionAnim = FindResource("CarPlayEngineIgnitionAnim") as Storyboard;
            ignitionAnim?.Begin(this);

            // 2. Await Preloading (Simulating Car System Diagnostics)
            await Task.Delay(1000); // Minimum visibility
            StartupStatusText.Text = "LOADING CORE TELEMETRY...";
            await mainVm.PreloadAllDataAsync();
            
            StartupStatusText.Text = "STARTING SERVICES...";
            mainVm.StartTimers();
            await Task.Delay(800);

            // 3. Final Navigation (to Home Grid)
            _ = mainVm.Navigate("Home");

            // 4. Car Startup Transition Animation
            var fadeOutOverlay = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.8));
            var fadeInInterface = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.0));
            
            fadeOutOverlay.Completed += (s, ev) => StartupOverlay.Visibility = Visibility.Collapsed;

            StartupOverlay.BeginAnimation(OpacityProperty, fadeOutOverlay);
            MainInterfaceGrid.BeginAnimation(OpacityProperty, fadeInInterface);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private System.Windows.Point _dockStartPoint;

        private void DockItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dockStartPoint = e.GetPosition(null);
        }

        private void DockItem_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _dockStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is System.Windows.Controls.Button button && button.CommandParameter != null)
                {
                    string viewName = button.CommandParameter.ToString();
                    string dragData = "Unpin:" + viewName;
                    DragDrop.DoDragDrop(button, dragData, System.Windows.DragDropEffects.Move);
                }
            }
        }

        private void MainContent_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && data.StartsWith("Unpin:"))
                {
                    e.Effects = System.Windows.DragDropEffects.Move;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void MainContent_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && data.StartsWith("Unpin:"))
                {
                    string viewName = data.Substring("Unpin:".Length);
                    var mainVm = DataContext as MainViewModel;
                    if (mainVm != null)
                    {
                        mainVm.UnpinFeatureCommand.Execute(viewName);
                    }
                }
            }
            e.Handled = true;
        }

        private void Dock_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string data = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (data != null && !data.StartsWith("Unpin:"))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void Dock_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
            {
                string viewName = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);
                if (viewName != null && !viewName.StartsWith("Unpin:"))
                {
                    var mainVm = DataContext as MainViewModel;
                    mainVm?.PinFeatureCommand.Execute(viewName);
                }
            }
            e.Handled = true;
        }
    }
}

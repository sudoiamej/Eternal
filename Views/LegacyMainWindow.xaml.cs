using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class LegacyMainWindow : Window
    {
        private bool _canClose = false;
        private NotifyIcon? _notifyIcon;

        /// <summary>
        /// Gets or sets whether the window is closing because of a UI swap.
        /// If true, the professional shutdown sequence and Application.Shutdown() will be bypassed.
        /// </summary>
        public bool IsSwappingUI { get; set; } = false;

        public LegacyMainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            InitializeTray();
            
            // Adjust window dimensions dynamically to fit under 100% and 125% DPI scale boundaries
            AdjustWindowSizeToWorkingArea();

            this.StateChanged += (s, e) =>
            {
                var vm = this.DataContext as MainViewModel;
                if (vm != null)
                {
                    if (this.WindowState == WindowState.Minimized)
                    {
                        vm.PauseBackgroundWork();
                    }
                    else
                    {
                        vm.ResumeBackgroundWork();
                    }
                }
            };
        }

        private void AdjustWindowSizeToWorkingArea()
        {
            try
            {
                double workingWidth = SystemParameters.WorkArea.Width;
                double workingHeight = SystemParameters.WorkArea.Height;

                // Base design dimensions are 1400 x 800. If the display working area is tight (e.g. 100% small screen or 125% scaled 1080p which gives 1536x832)
                if (workingWidth < 1450 || workingHeight < 850)
                {
                    this.Width = Math.Max(1024, workingWidth * 0.92);
                    this.Height = Math.Max(700, workingHeight * 0.88);
                }
            }
            catch { }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.T && 
                System.Windows.Input.Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt))
            {
                var authWindow = new Eternal.Views.Helpers.TestingAuthWindow();
                authWindow.Owner = this;
                if (authWindow.ShowDialog() == true)
                {
                    var vm = this.DataContext as MainViewModel;
                    vm?.ActivateTestingMode();
                }
            }
        }

        private void InitializeTray()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                _notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico")).Stream);
                _notifyIcon.Text = "Eternal System Intelligence (Legacy UI)";
                _notifyIcon.Visible = true;

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Show Eternal", null, (s, e) => ShowWindow());
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add("Exit", null, (s, e) => ForceExit());
                
                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            }
            catch { }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var vm = this.DataContext as MainViewModel;
            if (vm != null)
            {
                double scaleX = e.NewSize.Width / 1280.0;
                double scaleY = e.NewSize.Height / 720.0;
                double targetScale = Math.Min(scaleX, scaleY);
                vm.UpdateFitScale(Math.Max(0.5, Math.Min(2.0, targetScale)));
            }
        }

        private void Window_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                var vm = this.DataContext as MainViewModel;
                if (vm != null)
                {
                    if (e.Delta > 0)
                    {
                        vm.ZoomInCommand.Execute(null);
                    }
                    else
                    {
                        vm.ZoomOutCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
        }

        private void ForceExit()
        {
            _canClose = true;
            this.Close();
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_canClose || IsSwappingUI)
            {
                CleanupTray();
                return;
            }

            var vm = this.DataContext as MainViewModel;
            if (vm?.Settings?.MinimizeToTray == true)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            e.Cancel = true;
            await PerformProfessionalShutdown();
        }

        private async Task PerformProfessionalShutdown()
        {
            try
            {
                this.Hide();
                CleanupTray();
            }
            catch { }
            finally
            {
                _canClose = true;
                this.Close();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void CleanupTray()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
        private void OverlayEnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.AuthenticateWithPasswordCommand.Execute(OverlayPasswordBox.Password);
            }
        }

        private void OverlayPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.AuthenticateWithPasswordCommand.Execute(OverlayPasswordBox.Password);
                }
            }
        }

        private int _userGreetingClickCount = 0;
        private DateTime _lastUserGreetingClick = DateTime.MinValue;

        private void UserGreetingBadge_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((DateTime.Now - _lastUserGreetingClick).TotalSeconds > 1.8)
            {
                _userGreetingClickCount = 0;
            }

            _lastUserGreetingClick = DateTime.Now;
            _userGreetingClickCount++;

            if (_userGreetingClickCount >= 5)
            {
                _userGreetingClickCount = 0;
                var netUserWin = new Helpers.NetUserInspectorWindow();
                netUserWin.Owner = this;
                netUserWin.ShowDialog();
            }
        }
    }
}
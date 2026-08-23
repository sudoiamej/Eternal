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
        private bool _isFullscreen = false;
        private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
        private WindowState _previousWindowState = WindowState.Normal;

        public void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _previousWindowStyle = this.WindowStyle;
                _previousWindowState = this.WindowState;

                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
            else
            {
                this.WindowStyle = _previousWindowStyle;
                this.WindowState = _previousWindowState;
                _isFullscreen = false;
            }
        }
        private NotifyIcon? _notifyIcon;

        /// <summary>
        /// Gets or sets whether the window is closing because of a UI swap.
        /// If true, the professional shutdown sequence and Application.Shutdown() will be bypassed.
        /// </summary>
        public bool IsSwappingUI { get; set; } = false;

        private System.Windows.Threading.DispatcherTimer? _inactivityTimer;
        private DateTime _lastUserActivityTime = DateTime.Now;

        public LegacyMainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            InitializeTray();
            InitializeInactivityTimer();
            
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

        private readonly System.Windows.Input.Key[] _konamiCode = new[]
        {
            System.Windows.Input.Key.Up, System.Windows.Input.Key.Up,
            System.Windows.Input.Key.Down, System.Windows.Input.Key.Down,
            System.Windows.Input.Key.Left, System.Windows.Input.Key.Right,
            System.Windows.Input.Key.Left, System.Windows.Input.Key.Right,
            System.Windows.Input.Key.B, System.Windows.Input.Key.A
        };
        private int _konamiIndex = 0;

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var actualKey = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

            // Ignore standalone modifier key presses so they don't break the Konami sequence
            if (actualKey == System.Windows.Input.Key.LeftShift || actualKey == System.Windows.Input.Key.RightShift ||
                actualKey == System.Windows.Input.Key.LeftCtrl || actualKey == System.Windows.Input.Key.RightCtrl ||
                actualKey == System.Windows.Input.Key.LeftAlt || actualKey == System.Windows.Input.Key.RightAlt ||
                actualKey == System.Windows.Input.Key.Capital || actualKey == System.Windows.Input.Key.LWin ||
                actualKey == System.Windows.Input.Key.RWin)
            {
                return;
            }

            // Toggle Fullscreen on F11
            if (actualKey == System.Windows.Input.Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
                return;
            }

            // Testing Auth Hotkey: Ctrl + Shift + Alt + T
            if (actualKey == System.Windows.Input.Key.T && 
                System.Windows.Input.Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt))
            {
                var authWindow = new Eternal.Views.Helpers.TestingAuthWindow();
                authWindow.Owner = this;
                if (authWindow.ShowDialog() == true)
                {
                    var vm = this.DataContext as MainViewModel;
                    vm?.ActivateTestingMode();
                }
                return;
            }

            // BSOD Hotkey: Ctrl + Shift + Alt + B
            if (actualKey == System.Windows.Input.Key.B &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0 &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0 &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
            {
                var bsodWin = new Helpers.BsodSimulatorWindow();
                bsodWin.Owner = this;
                bsodWin.ShowDialog();
                return;
            }

            // ESC to close Matrix mode
            if (actualKey == System.Windows.Input.Key.Escape && MatrixOverlayGrid.Visibility == Visibility.Visible)
            {
                MatrixOverlayGrid.Visibility = Visibility.Collapsed;
                return;
            }

            // Konami Code Sequence
            if (actualKey == _konamiCode[_konamiIndex])
            {
                _konamiIndex++;
                if (_konamiIndex >= _konamiCode.Length)
                {
                    _konamiIndex = 0;
                    MatrixOverlayGrid.Visibility = MatrixOverlayGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            else if (actualKey == _konamiCode[0])
            {
                _konamiIndex = 1;
            }
            else
            {
                _konamiIndex = 0;
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

        private void InitializeInactivityTimer()
        {
            _inactivityTimer = new System.Windows.Threading.DispatcherTimer();
            _inactivityTimer.Interval = TimeSpan.FromSeconds(15);
            _inactivityTimer.Tick += InactivityTimer_Tick;
            _inactivityTimer.Start();

            this.PreviewMouseMove += ResetUserActivity;
            this.PreviewKeyDown += ResetUserActivity;
            this.PreviewMouseDown += ResetUserActivity;
        }

        private void ResetUserActivity(object sender, EventArgs e)
        {
            _lastUserActivityTime = DateTime.Now;
            if (DataContext is ViewModels.MainViewModel vm && !vm.IsAuthenticated)
            {
                vm.CheckCapsLockState();
            }
        }

        private bool _isSyncingPassword = false;

        private void PasswordEyeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.IsPasswordRevealed = !vm.IsPasswordRevealed;
                if (vm.IsPasswordRevealed)
                {
                    RevealedPasswordTextBox.Focus();
                    RevealedPasswordTextBox.SelectionStart = RevealedPasswordTextBox.Text.Length;
                }
                else
                {
                    OverlayPasswordBox.Focus();
                }
            }
        }

        private void OverlayPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            _isSyncingPassword = true;
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.RevealedPasswordText = OverlayPasswordBox.Password;
            }
            _isSyncingPassword = false;
        }

        private void RevealedPasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isSyncingPassword) return;
            _isSyncingPassword = true;
            OverlayPasswordBox.Password = RevealedPasswordTextBox.Text;
            _isSyncingPassword = false;
        }

        private async void RevealedPasswordTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    string pwd = RevealedPasswordTextBox.Text;
                    await vm.AuthenticateWithPasswordAsync(pwd);
                }
            }
        }

        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                if (!vm.IsAuthenticated || vm.IsPeMode) return;

                int timeoutMins = vm.Settings.InactivityTimeoutMinutes;
                if (timeoutMins <= 0) return;

                var idleDuration = DateTime.Now - _lastUserActivityTime;
                if (idleDuration.TotalMinutes >= timeoutMins)
                {
                    _lastUserActivityTime = DateTime.Now;
                    vm.PerformLockWorkstation(dueToInactivity: true);
                }
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
        private async void OverlayEnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                string pwd = OverlayPasswordBox.Password;
                await vm.AuthenticateWithPasswordAsync(pwd);
            }
        }

        private async void OverlayPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    string pwd = OverlayPasswordBox.Password;
                    await vm.AuthenticateWithPasswordAsync(pwd);
                }
            }
        }

        private async void OverlayWindowsHelloButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                await vm.TriggerWindowsHelloAsync();
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
            else
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.ToggleControlCenter();
                }
            }
        }

        private void ControlCenterBackdrop_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.IsControlCenterVisible = false;
            }
        }

        private int _logoClickCount = 0;
        private DateTime _lastLogoClick = DateTime.MinValue;

        private void EternalLogo_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((DateTime.Now - _lastLogoClick).TotalSeconds > 1.5)
            {
                _logoClickCount = 0;
            }

            _lastLogoClick = DateTime.Now;
            _logoClickCount++;

            if (_logoClickCount >= 3)
            {
                _logoClickCount = 0;
                var orbitalWin = new Helpers.OrbitalVisualizerWindow();
                orbitalWin.Owner = this;
                orbitalWin.ShowDialog();
            }
        }
    }
}
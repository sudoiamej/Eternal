using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class MainWindow : Window
    {
        private bool _canClose = false;
        private NotifyIcon? _notifyIcon;

        /// <summary>
        /// Gets or sets whether the window is closing because of a UI swap.
        /// If true, the professional shutdown sequence will be bypassed.
        /// </summary>
        public bool IsSwappingUI { get; set; } = false;

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.SourceInitialized += MainWindow_SourceInitialized;
            InitializeTray();
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            ApplyGlassmorphism();
        }

        private void ApplyGlassmorphism()
        {
            try
            {
                bool isPeMode = System.IO.Directory.Exists(@"X:\Windows\System32");
                if (isPeMode) return; // Disable glass in WinRE for stability

                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                var osVersion = Environment.OSVersion.Version;

                if (osVersion.Major >= 10 && osVersion.Build >= 22000)
                {
                    // Windows 11: Mica Effect
                    int trueValue = 1;
                    DwmSetWindowAttribute(hwnd, 38, ref trueValue, Marshal.SizeOf(typeof(int))); // DWMWA_MICA_EFFECT
                }
                else if (osVersion.Major >= 10)
                {
                    // Windows 10: Acrylic Blur
                    var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, GradientColor = 0x01000000 };
                    var accentStructSize = Marshal.SizeOf(accent);
                    var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                    Marshal.StructureToPtr(accent, accentPtr, false);

                    var data = new WindowCompositionAttributeData
                    {
                        Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                        SizeOfData = accentStructSize,
                        Data = accentPtr
                    };

                    SetWindowCompositionAttribute(hwnd, ref data);
                    Marshal.FreeHGlobal(accentPtr);
                }
                
                this.Background = System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Glassmorphism error: {ex.Message}");
            }
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
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico")).Stream);
            _notifyIcon.Text = "Eternal System Intelligence";
            _notifyIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Eternal", null, (s, e) => ShowWindow());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ForceExit());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Sidebar auto-collapse logic
            if (e.NewSize.Width < 1200)
            {
                if (MenuToggle.IsChecked == true) MenuToggle.IsChecked = false;
            }
            else
            {
                if (MenuToggle.IsChecked == false) MenuToggle.IsChecked = true;
            }

            // Note: Dynamic Auto-Scaling (proportional zoom) is disabled to maintain native 100% resolution.
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ForceExit()
        {
            _canClose = true;
            this.Close();
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_canClose || IsSwappingUI)
            {
                CleanupTray();
                return;
            }
            // Check if we should minimize to tray instead of closing
            var vm = this.DataContext as MainViewModel;
            if (vm?.Settings?.MinimizeToTray == true)
            {
                e.Cancel = true;
                this.Hide();
                _notifyIcon?.ShowBalloonTip(2000, "Eternal", "Still running in background to monitor system health.", ToolTipIcon.Info);
                return;
            }

            // Normal professional shutdown sequence
            e.Cancel = true;
            await PerformProfessionalShutdown();
        }

        private async Task PerformProfessionalShutdown()
        {
            try
            {
                // Instant hide to give the user immediate feedback
                this.Hide();

                // perform cleanup without artificial delays
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
    }
}

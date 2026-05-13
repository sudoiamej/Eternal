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
                // Base design width is 1400. Calculate proportional scale.
                double targetScale = e.NewSize.Width / 1400.0;
                vm.DisplayScale = Math.Max(0.5, Math.Min(2.0, targetScale));
            }
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
    }
}
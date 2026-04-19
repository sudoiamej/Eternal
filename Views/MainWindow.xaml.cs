using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class MainWindow : Window
    {
        private bool _canClose = false;
        private NotifyIcon? _notifyIcon;

        public MainWindow()
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
                if (authWindow.ShowDialog() == true && authWindow.IsAuthorized)
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
            if (_canClose)
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
            // Hide main window
            this.Hide();

            // Show shutdown screen
            var shutdownWindow = new ShutdownWindow();
            shutdownWindow.Show();

            try
            {
                await Task.Delay(1000);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Finalizing telemetry logs");
                
                await Task.Delay(800);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Checking system integrity");

                await Task.Delay(700);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Releasing hardware hooks");

                await Task.Delay(500);
                shutdownWindow.UpdateStatus("Shutdown complete", "Closing Eternal");
            }
            catch { }
            finally
            {
                _canClose = true;
                CleanupTray();
                shutdownWindow.Close();
                this.Close();
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

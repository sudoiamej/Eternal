using System;
using System.Threading.Tasks;
using System.Windows;

namespace Eternal.Views
{
    public partial class MainWindow : Window
    {
        private bool _canClose = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_canClose) return;

            // Prevent immediate closing
            e.Cancel = true;

            // Hide main window
            this.Hide();

            // Show shutdown screen
            var shutdownWindow = new ShutdownWindow();
            shutdownWindow.Show();

            try
            {
                // Sequence of "checks" and cleanup
                await Task.Delay(1000);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Finalizing telemetry logs");
                
                await Task.Delay(800);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Checking system integrity");

                await Task.Delay(700);
                shutdownWindow.UpdateStatus("Saving and cleaning up...", "Releasing hardware hooks");

                await Task.Delay(500);
                shutdownWindow.UpdateStatus("Shutdown complete", "Closing Eternal");
            }
            catch (Exception ex)
            {
                // Silently handle or log if needed, but we must ensure we close
                Console.WriteLine($"Shutdown error: {ex.Message}");
            }
            finally
            {
                _canClose = true;
                shutdownWindow.Close();
                this.Close();
            }
        }
    }
}

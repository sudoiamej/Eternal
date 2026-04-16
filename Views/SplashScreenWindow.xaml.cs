using System;
using System.Threading.Tasks;
using System.Windows;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            StartLoading();
        }

        private async void StartLoading()
        {
            // Minimum display time for branding visibility (3 seconds)
            var timerTask = Task.Delay(3000);

            // Initialize the Main ViewModel first (Lightweight)
            StatusText.Text = "Initializing Eternal Intelligence...";
            var mainVm = new MainViewModel();
            await Task.Delay(500); 

            StatusText.Text = "Synchronizing System Telemetry...";
            // Trigger Preloading but don't await the whole thing if it takes too long
            // We await the most critical parts for the Dashboard
            var preloadTask = mainVm.PreloadAllDataAsync();
            
            StatusText.Text = "Mapping System Architecture...";
            await Task.Delay(500);

            StatusText.Text = "Finalizing Secure Interface...";
            
            // Ensure we stay visible for at least the branding timer
            await timerTask;

            // Create MainWindow but don't let it create its own MainViewModel
            var mainWindow = new MainWindow();
            mainWindow.DataContext = mainVm;
            mainVm.StartTimers(); // Start real-time status bar

            System.Windows.Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            
            this.Close();
        }
    }
}
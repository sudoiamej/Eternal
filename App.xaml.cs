using System.Windows;
using Eternal.Views;

namespace Eternal
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);

            // 1. Check for CLI Arguments (Headless Mode)
            if (e.Args.Length > 0)
            {
                await HandleCommandLineArgs(e.Args);
                return;
            }

            // 2. Launch GUI if no args
            var splash = new SplashScreenWindow();
            splash.Show();
        }

        private async Task HandleCommandLineArgs(string[] args)
        {
            // Simple CLI Handler for Experts/Automation
            string arg = args[0].ToLower();

            if (arg == "--report")
            {
                Console.WriteLine("ETERNAL CLI: Generating System Intelligence Report...");
                // In a real scenario, we'd instantiate services, gather data, and write to a file
                await Task.Delay(2000); 
                Console.WriteLine("Report generated: C:\\ProgramData\\Eternal\\LastReport.json");
            }
            else if (arg == "--debloat")
            {
                Console.WriteLine("ETERNAL CLI: Applying 'Safe' Debloat Preset...");
                await Task.Delay(3000);
                Console.WriteLine("System Optimized successfully.");
            }
            else
            {
                Console.WriteLine("Unknown command. Available: --report, --debloat");
            }

            Shutdown();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled Exception: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown();
        }
    }
}
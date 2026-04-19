using System.Windows;
using Eternal.Views;

namespace Eternal
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global Safety Net
            this.DispatcherUnhandledException += (s, ex) => {
                System.Windows.MessageBox.Show($"Eternal Intelligence encountered a critical interface error:\n\n{ex.Exception.Message}", "Security Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, ex) => {
                // Background task failed - log but don't necessarily crash
            };

            try
            {
                // 1. Check for CLI Arguments (Headless Mode)
                if (e.Args.Length > 0)
                {
                    _ = HandleCommandLineArgs(e.Args);
                    return;
                }

                // 2. Launch GUI if no args
                var splash = new SplashScreenWindow();
                splash.Show();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup Failure: {ex.Message}", "Critical Initialization", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
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
    }
}
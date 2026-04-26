using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Eternal.Services.System;

namespace Eternal.Models
{
    public class ConsoleProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Executable { get; set; } = string.Empty;
        public string Icon { get; set; } = "Terminal";
    }

    public partial class ConsoleSession : ObservableObject, IDisposable
    {
        private readonly IConsoleService _consoleService;
        public string Id { get; } = Guid.NewGuid().ToString();
        
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _icon;
        [ObservableProperty] private string _currentInput = string.Empty;
        [ObservableProperty] private bool _isBusy;
        
        public ObservableCollection<string> OutputLines { get; } = new ObservableCollection<string>();

        public ConsoleSession(IConsoleService consoleService, string title, string icon)
        {
            _consoleService = consoleService;
            _title = title;
            _icon = icon;

            OutputLines.Add($"[SYSTEM] Initializing {title} session environment...");

            _consoleService.OutputReceived += (s, line) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add(line);
                    if (OutputLines.Count > 1000) OutputLines.RemoveAt(0);
                });
            };

            _consoleService.Exited += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add("[SESSION TERMINATED]");
                    IsBusy = false;
                });
            };
        }

        public async Task StartAsync(string executable)
        {
            IsBusy = true;
            await _consoleService.StartAsync(executable, Title);
            IsBusy = false;
        }

        public async Task SendCommandAsync(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            OutputLines.Add($"> {cmd}");
            await _consoleService.SendCommandAsync(cmd);
        }

        public void Dispose()
        {
            _consoleService.Stop();
            _consoleService.Dispose();
        }
    }
}

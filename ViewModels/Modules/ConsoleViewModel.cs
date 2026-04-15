using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class ConsoleViewModel : ObservableObject
    {
        private readonly IConsoleService _consoleService;

        public ObservableCollection<string> OutputLines { get; } = new ObservableCollection<string>();
        public ObservableCollection<ConsoleMacro> Macros { get; } = new ObservableCollection<ConsoleMacro>();
        
        [ObservableProperty] private string _currentInput = "";
        [ObservableProperty] private bool _isBusy;

        public ConsoleViewModel(IConsoleService consoleService)
        {
            _consoleService = consoleService;
            _consoleService.OutputReceived += (s, line) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLines.Add(line);
                    if (OutputLines.Count > 500) OutputLines.RemoveAt(0);
                });
            };

            InitializeMacros();
            StartConsoleCommand = new AsyncRelayCommand(StartConsoleAsync);
        }

        public IAsyncRelayCommand StartConsoleCommand { get; }

        private async Task StartConsoleAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            OutputLines.Add("[ETERNAL] Initializing Integrated Shell Environment...");
            await _consoleService.StartAsync();
            IsBusy = false;
        }

        [RelayCommand]
        private async Task SendCommand()
        {
            if (string.IsNullOrWhiteSpace(CurrentInput)) return;
            
            string cmd = CurrentInput;
            CurrentInput = "";
            OutputLines.Add($"> {cmd}");
            await _consoleService.SendCommandAsync(cmd);
        }

        [RelayCommand]
        private async Task RunMacro(ConsoleMacro macro)
        {
            if (macro == null) return;
            OutputLines.Add($"[MACRO] Running: {macro.Name}");
            await _consoleService.SendCommandAsync(macro.Command);
        }

        [RelayCommand]
        private void ClearConsole()
        {
            OutputLines.Clear();
            OutputLines.Add("[ETERNAL] Console Buffer Cleared.");
        }

        private void InitializeMacros()
        {
            Macros.Add(new ConsoleMacro("Network Status", "netstat -ano | select -first 20", "View active connections"));
            Macros.Add(new ConsoleMacro("Process Tree", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 15", "List top CPU consumers"));
            Macros.Add(new ConsoleMacro("System Info", "systeminfo", "Detailed Windows summary"));
            Macros.Add(new ConsoleMacro("IP Config", "ipconfig /all", "Detailed adapter info"));
            Macros.Add(new ConsoleMacro("DNS Cache", "ipconfig /displaydns", "View local DNS resolver cache"));
        }
    }

    public record ConsoleMacro(string Name, string Command, string Description);
}

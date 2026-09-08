using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Eternal.Services.System;
using Eternal.Models;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class ConsoleViewModel : BaseViewModel
    {
        private readonly ILoggingService _loggingService;
        private readonly ICliDetectionService _cliDetectionService;

        public ObservableCollection<ConsoleSession> Sessions { get; } = new ObservableCollection<ConsoleSession>();
        public ObservableCollection<ConsoleMacro> Macros { get; } = new ObservableCollection<ConsoleMacro>();
        public ObservableCollection<ShellDescriptor> DetectedShells { get; } = new ObservableCollection<ShellDescriptor>();
        
        [ObservableProperty] private ConsoleSession? _selectedSession;

        public ConsoleViewModel(ILoggingService loggingService, ICliDetectionService cliDetectionService)
        {
            _loggingService = loggingService;
            _cliDetectionService = cliDetectionService;
            
            InitializeProfiles();
            InitializeMacros();
            
            StartConsoleCommand = new AsyncRelayCommand(StartDefaultSessionAsync);
        }

        public IAsyncRelayCommand StartConsoleCommand { get; }

        private async Task StartDefaultSessionAsync()
        {
            if (Sessions.Count == 0 && DetectedShells.Count > 0)
            {
                await NewTabAsync(DetectedShells.First());
            }
        }

        [RelayCommand]
        private async Task NewTabAsync(ShellDescriptor shell)
        {
            if (shell == null) return;

            var session = new ConsoleSession(new WindowsConsoleService(), shell.Name, shell.Icon);
            Sessions.Add(session);
            SelectedSession = session;
            
            _loggingService.Log($"Console: Opening new {shell.Name} session.");
            await session.StartAsync(shell.ExecutablePath, shell.Arguments);
        }

        [RelayCommand]
        private void CloseTab(ConsoleSession session)
        {
            if (session == null) return;
            
            session.Dispose();
            Sessions.Remove(session);
            
            if (SelectedSession == null && Sessions.Any())
            {
                SelectedSession = Sessions.Last();
            }
        }

        [RelayCommand]
        private async Task SendCommand()
        {
            if (SelectedSession == null) return;
            await SelectedSession.SendCommandAsync(SelectedSession.CurrentInput);
            SelectedSession.CurrentInput = "";
        }

        [RelayCommand]
        private async Task RunMacro(ConsoleMacro macro)
        {
            if (macro == null || SelectedSession == null) return;
            _loggingService.Log($"Console: Running macro '{macro.Name}' in active session.");
            await SelectedSession.SendCommandAsync(macro.Command);
        }

        [RelayCommand]
        private void ClearConsole()
        {
            SelectedSession?.OutputLines.Clear();
            SelectedSession?.OutputLines.Add("[ETERNAL] Console Buffer Cleared.");
        }

        private void InitializeProfiles()
        {
            DetectedShells.Clear();
            var detected = _cliDetectionService.DetectInstalledShells();
            foreach (var shell in detected)
            {
                DetectedShells.Add(shell);
            }
        }

        private void InitializeMacros()
        {
            Macros.Add(new ConsoleMacro("Network Status", "netstat -ano", "View active connections"));
            Macros.Add(new ConsoleMacro("Process Tree", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 15", "List top CPU consumers"));
            Macros.Add(new ConsoleMacro("System Info", "systeminfo", "Detailed Windows summary"));
            Macros.Add(new ConsoleMacro("IP Config", "ipconfig /all", "Detailed adapter info"));
            Macros.Add(new ConsoleMacro("DNS Cache", "ipconfig /displaydns", "View local DNS resolver cache"));
        }
    }

    public record ConsoleMacro(string Name, string Command, string Description);
}

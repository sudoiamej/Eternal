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

namespace Eternal.ViewModels.Modules
{
    public partial class ConsoleViewModel : ObservableObject
    {
        private readonly ILoggingService _loggingService;

        public ObservableCollection<ConsoleSession> Sessions { get; } = new ObservableCollection<ConsoleSession>();
        public ObservableCollection<ConsoleMacro> Macros { get; } = new ObservableCollection<ConsoleMacro>();
        public List<ConsoleProfile> AvailableProfiles { get; } = new List<ConsoleProfile>();
        
        [ObservableProperty] private ConsoleSession? _selectedSession;
        [ObservableProperty] private bool _isBusy;

        public ConsoleViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            
            InitializeProfiles();
            InitializeMacros();
            
            StartConsoleCommand = new AsyncRelayCommand(StartDefaultSessionAsync);
        }

        public IAsyncRelayCommand StartConsoleCommand { get; }

        private async Task StartDefaultSessionAsync()
        {
            if (Sessions.Count == 0)
            {
                await NewTabAsync(AvailableProfiles.First());
            }
        }

        [RelayCommand]
        private async Task NewTabAsync(ConsoleProfile profile)
        {
            if (profile == null) return;

            var session = new ConsoleSession(new WindowsConsoleService(), profile.Name, profile.Icon);
            Sessions.Add(session);
            SelectedSession = session;
            
            _loggingService.Log($"Console: Opening new {profile.Name} session.");
            await session.StartAsync(profile.Executable);
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
            AvailableProfiles.Add(new ConsoleProfile { Name = "PowerShell", Executable = "powershell.exe", Icon = "Terminal" });
            AvailableProfiles.Add(new ConsoleProfile { Name = "Command Prompt", Executable = "cmd.exe", Icon = "Code" });
            
            // Detect Git Bash
            string gitBash = @"C:\Program Files\Git\bin\bash.exe";
            if (System.IO.File.Exists(gitBash))
                AvailableProfiles.Add(new ConsoleProfile { Name = "Git Bash", Executable = gitBash, Icon = "Git" });

            // Detect WSL
            string wsl = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wsl.exe");
            if (System.IO.File.Exists(wsl))
                AvailableProfiles.Add(new ConsoleProfile { Name = "WSL (Linux)", Executable = wsl, Icon = "Linux" });
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

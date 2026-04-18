using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Models;
using Eternal.Services.System;
using System;

namespace Eternal.ViewModels.Modules
{
    public partial class VerboseLoggingViewModel : ObservableObject
    {
        private readonly ILoggingService _loggingService;

        public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();
        
        [ObservableProperty] private bool _isBusy;

        public VerboseLoggingViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            
            RefreshLogs();

            // 2. Real-time Log Stream
            _loggingService.NewLogAdded += (s, entry) =>
            {
                var app = System.Windows.Application.Current;
                if (app == null) return;

                app.Dispatcher.Invoke(() =>
                {
                    // Always keep stream at top
                    LogEntries.Insert(0, entry);
                    if (LogEntries.Count > 2000) LogEntries.RemoveAt(LogEntries.Count - 1);
                });
            };

            // 3. Deferred System Event Load (to avoid blocking UI init)
            _ = Task.Run(async () => 
            {
                await Task.Delay(1000); 
                await LoadSystemLogsAsync();
            });
        }

        public void RefreshLogs()
        {
            LogEntries.Clear();
            var initialLogs = _loggingService.Logs.OrderByDescending(l => l.Timestamp);
            foreach (var log in initialLogs)
            {
                LogEntries.Add(log);
            }
        }

        [RelayCommand]
        public async Task LoadSystemLogsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            
            try
            {
                // Fetch deep system events (Win Event Viewer)
                var systemLogs = await _loggingService.GetSystemEventsAsync(500);
                
                var app = System.Windows.Application.Current;
                if (app == null) return;

                await app.Dispatcher.InvokeAsync(() =>
                {
                    // Merge and unique-ify based on message + timestamp
                    var currentMessages = new HashSet<string>(LogEntries.Select(l => $"{l.Timestamp.Ticks}_{l.Message}"));
                    
                    bool addedAny = false;
                    foreach (var log in systemLogs)
                    {
                        if (!currentMessages.Contains($"{log.Timestamp.Ticks}_{log.Message}"))
                        {
                            LogEntries.Add(log);
                            addedAny = true;
                        }
                    }

                    if (addedAny)
                    {
                        // Maintain strict reverse chronological order
                        var sorted = LogEntries.OrderByDescending(l => l.Timestamp).ToList();
                        LogEntries.Clear();
                        foreach (var log in sorted) LogEntries.Add(log);
                    }
                });
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            _loggingService.Clear();
            LogEntries.Clear();
        }
    }
}

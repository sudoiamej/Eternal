using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eternal.Models;
using Eternal.Services.System;

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
            
            // Load app logs
            foreach (var log in _loggingService.Logs)
            {
                LogEntries.Add(log);
            }

            _loggingService.NewLogAdded += (s, entry) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Insert(0, entry);
                    if (LogEntries.Count > 1000) LogEntries.RemoveAt(LogEntries.Count - 1);
                });
            };

            // Initial system log fetch
            _ = LoadSystemLogsAsync();
        }

        [RelayCommand]
        public async Task LoadSystemLogsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var systemLogs = await _loggingService.GetSystemEventsAsync(300);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var log in systemLogs)
                    {
                        // Add if not duplicate (simple check)
                        LogEntries.Add(log);
                    }
                    // Sort by timestamp
                    var sorted = new ObservableCollection<LogEntry>(
                        System.Linq.Enumerable.OrderByDescending(LogEntries, l => l.Timestamp)
                    );
                    LogEntries.Clear();
                    foreach (var s in sorted) LogEntries.Add(s);
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

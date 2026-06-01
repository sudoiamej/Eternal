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

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class VerboseLoggingViewModel : BaseViewModel
    {
        private readonly ILoggingService _loggingService;

        public ObservableCollection<LogEntry> ActionLogEntries { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> EventViewerEntries { get; } = new ObservableCollection<LogEntry>();
        
        [ObservableProperty] private bool _isBusy;

        public VerboseLoggingViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public override void Activate()
        {
            RefreshLogs();
            _loggingService.NewLogAdded += OnNewLogAdded;
            
            // 2. Initial Windows Event Load
            _ = Task.Run(async () => 
            {
                await Task.Delay(500); 
                await LoadSystemLogsAsync();
            });
        }

        public override void Deactivate()
        {
            _loggingService.NewLogAdded -= OnNewLogAdded;
            base.Deactivate();
        }

        public override void ReleaseMemory()
        {
            ActionLogEntries.Clear();
            EventViewerEntries.Clear();
        }

        private void OnNewLogAdded(object? sender, LogEntry entry)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            _ = app.Dispatcher.InvokeAsync(() =>
            {
                ActionLogEntries.Insert(0, entry);
                if (ActionLogEntries.Count > 2000) ActionLogEntries.RemoveAt(ActionLogEntries.Count - 1);
            });
        }

        public void RefreshLogs()
        {
            ActionLogEntries.Clear();
            var initialLogs = _loggingService.Logs.OrderByDescending(l => l.Timestamp);
            foreach (var log in initialLogs)
            {
                ActionLogEntries.Add(log);
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
                    EventViewerEntries.Clear();
                    foreach (var log in systemLogs)
                    {
                        EventViewerEntries.Add(log);
                    }
                });
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            _loggingService.Clear();
            ActionLogEntries.Clear();
            EventViewerEntries.Clear();
        }
    }
}

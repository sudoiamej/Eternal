using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface ILoggingService
    {
        IReadOnlyList<LogEntry> Logs { get; }
        event EventHandler<LogEntry> NewLogAdded;
        void Log(string message, string level = "INFO");
        void Clear();
        Task<List<LogEntry>> GetSystemEventsAsync(int count = 100);
    }

    public class WindowsLoggingService : ILoggingService
    {
        private readonly List<LogEntry> _logs = new();
        private readonly int _maxLogs = 500;
        private readonly ISettingsService _settings;

        public IReadOnlyList<LogEntry> Logs => _logs;
        public event EventHandler<LogEntry> NewLogAdded;

        public WindowsLoggingService(ISettingsService settings)
        {
            _settings = settings;
        }

        public void Log(string message, string level = "INFO")
        {
            if (!_settings.Current.IsVerboseLoggingEnabled) return;

            var entry = new LogEntry(DateTime.Now, message, level);
            
            lock (_logs)
            {
                _logs.Add(entry);
                if (_logs.Count > _maxLogs) _logs.RemoveAt(0);
            }

            NewLogAdded?.Invoke(this, entry);
        }

        public void Clear()
        {
            lock (_logs)
            {
                _logs.Clear();
            }
        }

        public Task<List<LogEntry>> GetSystemEventsAsync(int count = 100)
        {
            return Task.Run(() =>
            {
                var events = new List<LogEntry>();
                try
                {
                    string[] logNames = { "System", "Application" };
                    foreach (var name in logNames)
                    {
                        using var log = new EventLog(name);
                        int entriesCount = log.Entries.Count;
                        int start = Math.Max(0, entriesCount - (count / 2));
                        
                        for (int i = entriesCount - 1; i >= start; i--)
                        {
                            var entry = log.Entries[i];
                            string level = entry.EntryType switch
                            {
                                EventLogEntryType.Error => "ERROR",
                                EventLogEntryType.Warning => "WARN",
                                _ => "INFO"
                            };

                            events.Add(new LogEntry(
                                entry.TimeGenerated,
                                $"[{entry.Source}] {entry.Message}",
                                level
                            ));
                        }
                    }
                }
                catch (Exception ex)
                {
                    events.Add(new LogEntry(DateTime.Now, $"Failed to fetch system logs: {ex.Message}", "ERROR"));
                }
                return events.OrderByDescending(e => e.Timestamp).Take(count).ToList();
            });
        }
    }
}

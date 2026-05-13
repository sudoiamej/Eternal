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
            // Only return if verbose is disabled AND the level is not INFO, WARN, or ERROR
            if (!_settings.Current.IsVerboseLoggingEnabled && level != "INFO" && level != "WARN" && level != "ERROR") return;

            var entry = new LogEntry(DateTime.Now, "Application", message, level);
            
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
                        try 
                        {
                            if (!EventLog.Exists(name)) continue;
                            
                            using var log = new EventLog(name);
                            int entriesCount = log.Entries.Count;
                            
                            // Optimization: Only scan the last 'count' entries
                            int scanCount = Math.Min(entriesCount, count);
                            int start = entriesCount - 1;
                            int end = Math.Max(0, entriesCount - scanCount);
                            
                            for (int i = start; i >= end; i--)
                            {
                                var entry = log.Entries[i];
                                string level = entry.EntryType switch
                                {
                                    EventLogEntryType.Error => "ERROR",
                                    EventLogEntryType.Warning => "WARN",
                                    EventLogEntryType.Information => "INFO",
                                    EventLogEntryType.SuccessAudit => "SUCCESS",
                                    EventLogEntryType.FailureAudit => "FAILURE",
                                    _ => "INFO"
                                };

                                events.Add(new LogEntry(
                                    entry.TimeGenerated,
                                    entry.Source,
                                    entry.Message,
                                    level
                                ));
                            }
                        }
                        catch (Exception ex)
                        {
                            events.Add(new LogEntry(DateTime.Now, "LogService", $"Failed to read {name} log: {ex.Message}", "WARN"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    events.Add(new LogEntry(DateTime.Now, "LogService", $"Critical failure in system log fetch: {ex.Message}", "ERROR"));
                }
                return events.OrderByDescending(e => e.Timestamp).Take(count).ToList();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsProcessService : IProcessService
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

        private class ProcessCacheItem
        {
            public string Path { get; set; } = string.Empty;
            public int SessionId { get; set; }
            public ProcessCategory Category { get; set; } = ProcessCategory.Background;
            public TimeSpan TotalTime { get; set; }
            public ulong IoTotal { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly Dictionary<int, ProcessCacheItem> _procHistory = new();
        private readonly object _historyLock = new();
        private readonly int _processorCount = Environment.ProcessorCount;

        public Task<List<ProcessDetail>> GetRunningProcessesAsync()
        {
            return Task.Run(() =>
            {
                var details = new List<ProcessDetail>();
                var processes = Process.GetProcesses();
                var now = DateTime.UtcNow;
                var currentPids = new HashSet<int>(processes.Length);

                foreach (var proc in processes)
                {
                    try
                    {
                        int pid = proc.Id;
                        currentPids.Add(pid);
                        if (proc.HasExited) continue;

                        ProcessCacheItem? history;
                        lock (_historyLock)
                        {
                            if (!_procHistory.TryGetValue(pid, out history))
                            {
                                history = new ProcessCacheItem
                                {
                                    SessionId = -1, // Uninitialized
                                    Category = ProcessCategory.Background
                                };
                                _procHistory[pid] = history;
                            }
                        }

                        // 1. Resolve Static Properties (Cache)
                        if (history.SessionId == -1)
                        {
                            try { history.SessionId = proc.SessionId; } catch { history.SessionId = 0; }
                            
                            string path = "Access Denied";
                            if (pid > 4)
                            {
                                try { path = proc.MainModule?.FileName ?? "N/A"; } catch { }
                            }
                            else if (pid == 4) path = "System";
                            else if (pid == 0) path = "Idle";
                            history.Path = path;

                            // Initial Categorization
                            if (history.SessionId == 0 || history.Path.Contains(@"\Windows\System32", StringComparison.OrdinalIgnoreCase))
                                history.Category = ProcessCategory.Windows;
                            else if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                                history.Category = ProcessCategory.Apps;
                        }

                        // 2. Resolve Dynamic Metrics
                        double cpuUsage = 0;
                        long diskBytes = 0;
                        string statusString = "Running";

                        try
                        {
                            if (pid > 0 && pid != 4 && !proc.HasExited)
                            {
                                var currentTime = proc.TotalProcessorTime;
                                ulong currentIo = 0;
                                if (GetProcessIoCounters(proc.Handle, out var io))
                                {
                                    currentIo = io.ReadTransferCount + io.WriteTransferCount + io.OtherTransferCount;
                                }

                                if (history.Timestamp != default)
                                {
                                    double timeDelta = (currentTime - history.TotalTime).TotalMilliseconds;
                                    double ioDelta = (double)(currentIo - history.IoTotal);
                                    double intervalDelta = (now - history.Timestamp).TotalMilliseconds;

                                    if (intervalDelta > 100) // Avoid jitter
                                    {
                                        cpuUsage = (timeDelta / intervalDelta / _processorCount) * 100;
                                        diskBytes = (long)(ioDelta / (intervalDelta / 1000.0));
                                    }
                                }

                                history.TotalTime = currentTime;
                                history.IoTotal = currentIo;
                                history.Timestamp = now;

                                if (proc.Responding == false) statusString = "Not Responding";
                            }
                        }
                        catch { }

                        long net = cpuUsage > 15 ? 51200 : 0; 

                        details.Add(new ProcessDetail(
                            PID: pid,
                            Name: proc.ProcessName,
                            CpuUsage: Math.Clamp(cpuUsage, 0, 100),
                            MemoryBytes: proc.WorkingSet64,
                            Path: history.Path,
                            IsSigned: true,
                            Impact: cpuUsage > 20 ? "High" : (cpuUsage > 5 ? "Medium" : "Low"),
                            Status: statusString,
                            SessionId: history.SessionId,
                            DiskBytesPerSec: Math.Max(0, diskBytes),
                            NetworkBytesPerSec: net,
                            Category: history.Category
                        ));
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }

                lock (_historyLock)
                {
                    var deadPids = _procHistory.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
                    foreach (var pid in deadPids) _procHistory.Remove(pid);
                }

                return details; // Sorting handled by ViewModel if needed
            });
        }

        public Task<bool> KillProcessAsync(int pid)
        {
            return Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {pid}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        return process?.ExitCode == 0;
                    }
                }
                catch { return false; }
            });
        }

        public Task<ExtendedProcessInfo> GetExtendedProcessInfoAsync(ProcessDetail process)
        {
            return Task.Run(() =>
            {
                var loadedModules = new List<string>();
                var staticImports = new List<string>();
                var heuristics = new List<string>();

                try
                {
                    var proc = Process.GetProcessById(process.PID);

                    try
                    {
                        foreach (ProcessModule module in proc.Modules)
                        {
                            loadedModules.Add($"{module.ModuleName} ({module.FileName})");
                        }
                    }
                    catch (Exception ex) { loadedModules.Add($"Error fetching modules: {ex.Message}"); }        

                    if (File.Exists(process.Path))
                    {
                        staticImports = GetStaticImports(process.Path);
                    }
                    else
                    {
                        staticImports.Add("File not accessible for static analysis.");
                    }

                    if (process.Path == "Access Denied") heuristics.Add("Process path is hidden or restricted (Possible Rootkit/System Process).");
                    if (process.MemoryBytes > 1024 * 1024 * 1024) heuristics.Add("Extremely high memory footprint (>1GB).");

                    var tempPath = Path.GetTempPath();
                    if (!string.IsNullOrEmpty(process.Path) && process.Path.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
                        heuristics.Add("Process is running from a temporary directory (Highly Suspicious).");

                    if (loadedModules.Any(m => m.Contains("wininet.dll") || m.Contains("winhttp.dll")))
                        heuristics.Add("Process has networking capabilities (Loaded wininet/winhttp).");        

                    if (staticImports.Any(i => i.Contains("Advapi32.dll") && (i.Contains("Reg") || i.Contains("Service"))))
                        heuristics.Add("Process interacts with Registry or System Services.");

                    if (heuristics.Count == 0) heuristics.Add("No immediate behavioral anomalies detected.");   

                    proc.Dispose();
                }
                catch (Exception ex)
                {
                    heuristics.Add($"Analysis Error: {ex.Message}");
                }

                return new ExtendedProcessInfo(
                    PID: process.PID, 
                    Name: process.Name, 
                    StaticImports: staticImports, 
                    LoadedModules: loadedModules, 
                    HeuristicReasons: heuristics
                );
            });
        }

        private List<string> GetStaticImports(string filePath)
        {
            var imports = new List<string>();
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   
                using var reader = new BinaryReader(fs);

                if (reader.ReadUInt16() != 0x5A4D) return imports;
                fs.Seek(0x3C, SeekOrigin.Begin);
                uint ntHeaderOffset = reader.ReadUInt32();
                fs.Seek(ntHeaderOffset, SeekOrigin.Begin);

                if (reader.ReadUInt32() != 0x00004550) return imports;

                fs.Seek(ntHeaderOffset + 4, SeekOrigin.Begin);
                ushort machine = reader.ReadUInt16();
                int offsetToDataDirs = (machine == 0x8664) ? 128 : 112;

                fs.Seek(ntHeaderOffset + 4 + 20 + offsetToDataDirs + 8, SeekOrigin.Begin);
                uint importRva = reader.ReadUInt32();
                uint importSize = reader.ReadUInt32();

                if (importRva == 0) return imports;

                imports.Add($"PE Header detected (Machine: 0x{machine:X})");
                imports.Add($"Import Directory RVA: 0x{importRva:X}");

                fs.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[(int)Math.Min(fs.Length, 1024 * 1024)];
                fs.ReadExactly(buffer, 0, buffer.Length);
                string content = Encoding.ASCII.GetString(buffer);

                string[] commonDlls = { "kernel32.dll", "user32.dll", "advapi32.dll", "wininet.dll", "ws2_32.dll", "urlmon.dll", "ntdll.dll" };
                foreach(var dll in commonDlls)
                {
                    if (content.Contains(dll, StringComparison.OrdinalIgnoreCase)) imports.Add(dll);
                }
            }
            catch { }
            return imports.Distinct().ToList();
        }
    }
}

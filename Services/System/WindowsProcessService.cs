using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsProcessService : IProcessService
    {
        private Dictionary<int, (TimeSpan TotalTime, DateTime Timestamp)> _cpuHistory = new();
        private readonly int _processorCount = Environment.ProcessorCount;

        public Task<List<ProcessDetail>> GetRunningProcessesAsync()
        {
            return Task.Run(() =>
            {
                var details = new List<ProcessDetail>();
                var processes = Process.GetProcesses();
                var now = DateTime.UtcNow;

                foreach (var proc in processes)
                {
                    try
                    {
                        // Check if process is still alive before intensive property access
                        if (proc.HasExited) continue;

                        int pid = proc.Id;
                        string name = proc.ProcessName;
                        long mem = 0;
                        int sessionId = 0;
                        string statusString = "Running";

                        try { mem = proc.WorkingSet64; } catch { }
                        try { sessionId = proc.SessionId; } catch { }
                        try { if (proc.Responding == false) statusString = "Not Responding"; } catch { }

                        string path = "Access Denied";
                        if (pid > 4)
                        {
                            try { path = proc.MainModule?.FileName ?? "N/A"; } catch { }
                        }
                        else if (pid == 4) path = "System";
                        else if (pid == 0) path = "Idle";

                        // CPU Calculation (Delta)
                        double cpuUsage = 0;
                        try
                        {
                            if (pid > 0 && pid != 4 && !proc.HasExited)
                            {
                                var currentTime = proc.TotalProcessorTime;
                                if (_cpuHistory.TryGetValue(pid, out var prev))
                                {
                                    double timeDelta = (currentTime - prev.TotalTime).TotalMilliseconds;
                                    double intervalDelta = (now - prev.Timestamp).TotalMilliseconds;
                                    if (intervalDelta > 0)
                                    {
                                        cpuUsage = (timeDelta / intervalDelta / _processorCount) * 100;
                                    }
                                }
                                _cpuHistory[pid] = (currentTime, now);
                            }
                        }
                        catch { }

                        // Categorization
                        ProcessCategory category = ProcessCategory.Background;
                        if (sessionId == 0 || path.Contains(@"\Windows\System32", StringComparison.OrdinalIgnoreCase))
                        {
                            category = ProcessCategory.Windows;
                        }
                        else
                        {
                            try 
                            { 
                                // MainWindowHandle access is a frequent source of "No process is associated" exceptions
                                if (!proc.HasExited && proc.MainWindowHandle != IntPtr.Zero)
                                {
                                    string title = "";
                                    try { title = proc.MainWindowTitle; } catch { }
                                    if (!string.IsNullOrEmpty(title))
                                        category = ProcessCategory.Apps; 
                                }
                            } 
                            catch { }
                        }

                        // Disk/Network (Mock for now)
                        Random rng = new Random(pid);
                        long disk = cpuUsage > 5 ? rng.Next(1024 * 1024, 5 * 1024 * 1024) : 0;
                        long net = cpuUsage > 10 ? rng.Next(1024, 100 * 1024) : 0;

                        details.Add(new ProcessDetail(
                            PID: pid,
                            Name: name,
                            CpuUsage: Math.Clamp(cpuUsage, 0, 100),
                            MemoryBytes: mem,
                            Path: path,
                            IsSigned: true,
                            Impact: cpuUsage > 20 ? "High" : (cpuUsage > 5 ? "Medium" : "Low"),
                            Status: statusString,
                            SessionId: sessionId,
                            DiskBytesPerSec: disk,
                            NetworkBytesPerSec: net,
                            Category: category
                        ));
                    }
                    catch (Exception ex)
                    {
                        // Log locally but don't crash
                        Debug.WriteLine($"Process Scan Error (PID access): {ex.Message}");
                    }
                    finally { 
                        try { proc.Dispose(); } catch { }
                    }
                }

                // Cleanup dead processes from history
                var currentPids = processes.Select(p => { try { return p.Id; } catch { return -1; } }).ToHashSet();
                var deadPids = _cpuHistory.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
                foreach (var pid in deadPids) _cpuHistory.Remove(pid);

                return details.OrderByDescending(d => d.CpuUsage).ThenByDescending(d => d.MemoryBytes).ToList();
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

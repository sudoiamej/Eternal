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
        public Task<List<ProcessDetail>> GetRunningProcessesAsync()
        {
            return Task.Run(() =>
            {
                var details = new List<ProcessDetail>();
                var processes = Process.GetProcesses();

                foreach (var proc in processes)
                {
                    try
                    {
                        int pid = proc.Id;
                        string name = proc.ProcessName;
                        long mem = 0;
                        try { mem = proc.WorkingSet64; } catch { }
                        
                        string path = "Access Denied";
                        if (pid > 4) // Skip System and Idle
                        {
                            try { path = proc.MainModule?.FileName ?? "N/A"; } catch { }
                        }
                        else if (pid == 4) path = "System";
                        else if (pid == 0) path = "Idle";

                        // Simple impact logic
                        string impact = "Low";
                        if (mem > 500 * 1024 * 1024) impact = "Medium";
                        if (mem > 1024 * 1024 * 1024) impact = "High";

                        details.Add(new ProcessDetail(
                            pid,
                            name,
                            0, 
                            mem,
                            path,
                            true, 
                            impact
                        ));
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }
                return details.OrderByDescending(d => d.MemoryBytes).ToList();
            });
        }

        public Task<bool> KillProcessAsync(int pid)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Using taskkill /F /T /PID to forcefully terminate process and its children
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
                    var proc = Process.GetProcessById(process.Id);
                    
                    // 1. Loaded Modules
                    try
                    {
                        foreach (ProcessModule module in proc.Modules)
                        {
                            loadedModules.Add($"{module.ModuleName} ({module.FileName})");
                        }
                    }
                    catch (Exception ex) { loadedModules.Add($"Error fetching modules: {ex.Message}"); }

                    // 2. Static Imports (PE Parsing)
                    if (File.Exists(process.Path))
                    {
                        staticImports = GetStaticImports(process.Path);
                    }
                    else
                    {
                        staticImports.Add("File not accessible for static analysis.");
                    }

                    // 3. Heuristics
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

                }
                catch (Exception ex)
                {
                    heuristics.Add($"Analysis Error: {ex.Message}");
                }

                return new ExtendedProcessInfo(process.Id, process.Name, staticImports, loadedModules, heuristics);
            });
        }

        private List<string> GetStaticImports(string filePath)
        {
            var imports = new List<string>();
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new BinaryReader(fs);

                // DOS Header
                if (reader.ReadUInt16() != 0x5A4D) return imports; // MZ
                fs.Seek(0x3C, SeekOrigin.Begin);
                uint ntHeaderOffset = reader.ReadUInt32();
                fs.Seek(ntHeaderOffset, SeekOrigin.Begin);

                // NT Header
                if (reader.ReadUInt32() != 0x00004550) return imports; // PE\0\0
                
                fs.Seek(ntHeaderOffset + 4 + 20 + 96, SeekOrigin.Begin); // Skip FileHeader and OptionalHeader fixed parts to reach Data Directories
                // OptionalHeader is 96 or 112 bytes before data dirs for PE32/PE32+
                // Let's use a simpler approach: seek to import directory directly
                
                // Re-read machine type to detect PE32+
                fs.Seek(ntHeaderOffset + 4, SeekOrigin.Begin);
                ushort machine = reader.ReadUInt16();
                int offsetToDataDirs = (machine == 0x8664) ? 128 : 112; // 64bit vs 32bit

                fs.Seek(ntHeaderOffset + 4 + 20 + offsetToDataDirs + 8, SeekOrigin.Begin); // Import Directory is the 2nd entry (offset 8)
                uint importRva = reader.ReadUInt32();
                uint importSize = reader.ReadUInt32();

                if (importRva == 0) return imports;

                // For a proper parser, we'd need to map RVA to File Offset using Section Headers.
                // Since this is a lightweight diagnostic tool, we'll use a simplified heuristic or just report success of finding the table.
                imports.Add($"PE Header detected (Machine: 0x{machine:X})");
                imports.Add($"Import Directory RVA: 0x{importRva:X}");
                
                // Add common suspicious imports if found in strings (heuristic fallback)
                fs.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[(int)Math.Min(fs.Length, 1024 * 1024)]; // Read first MB
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
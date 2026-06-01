using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsBootService : IBootService
    {
        public async Task<List<BootRecord>> GetBootRecordsAsync()
        {
            return await Task.Run(() =>
            {
                var records = new List<BootRecord>();
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "bcdedit",
                        Arguments = "/enum",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return records;
                        
                        string output = process.StandardOutput.ReadToEnd();
                        records = ParseBcdOutput(output);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"BCD Error: {ex.Message}");
                }
                return records;
            });
        }

        private List<BootRecord> ParseBcdOutput(string output)
        {
            var records = new List<BootRecord>();
            var sections = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                if (!section.Contains("identifier", StringComparison.OrdinalIgnoreCase)) continue;

                var record = new BootRecord();
                var lines = section.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key.ToLower())
                    {
                        case "identifier": record.Identifier = value; break;
                        case "device": record.Device = value; break;
                        case "path": record.Path = value; break;
                        case "description": record.Description = value; break;
                        case "locale": record.Locale = value; break;
                        case "inherit": record.Inherit = value; break;
                        case "osdevice": record.OsDevice = value; break;
                        case "systemroot": record.SystemRoot = value; break;
                        case "resumeobject": record.ResumeObject = value; break;
                        case "nx": record.Nx = value; break;
                        case "bootmenupolicy": record.BootMenuPolicy = value; break;
                        case "safeboot": record.SafeBoot = value; break;
                    }
                }
                
                if (!string.IsNullOrEmpty(record.Identifier))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        public async Task<int> GetBootTimeoutAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "bcdedit",
                        Arguments = "/enum {bootmgr}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return 30;
                        string output = process.StandardOutput.ReadToEnd();
                        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (line.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2 && int.TryParse(parts[1], out int seconds))
                                {
                                    return seconds;
                                }
                            }
                        }
                    }
                }
                catch { }
                return 30; // fallback default
            });
        }

        public async Task<bool> SetBootTimeoutAsync(int seconds)
        {
            return await RunBcdeditCommandAsync($"/timeout {seconds}");
        }

        public async Task<bool> ToggleSafeBootAsync(string identifier, bool enable)
        {
            if (enable)
            {
                return await RunBcdeditCommandAsync($"/set {identifier} safeboot minimal");
            }
            else
            {
                return await RunBcdeditCommandAsync($"/deletevalue {identifier} safeboot");
            }
        }

        public async Task<bool> DeleteBootEntryAsync(string identifier)
        {
            if (string.IsNullOrEmpty(identifier) ||
                string.Equals(identifier, "{current}", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(identifier, "{bootmgr}", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return await RunBcdeditCommandAsync($"/delete {identifier} /f");
        }

        private async Task<bool> RunBcdeditCommandAsync(string arguments)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "bcdedit",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return false;
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"bcdedit execution error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

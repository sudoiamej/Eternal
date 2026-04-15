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
                    }
                }
                
                if (!string.IsNullOrEmpty(record.Identifier))
                {
                    records.Add(record);
                }
            }

            return records;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsDismService : IDismService
    {
        public async Task<WimFileDetails?> GetImageInfoAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!global::System.IO.File.Exists(filePath)) return null;

                    var details = new WimFileDetails { FilePath = filePath };
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = $"/Get-WimInfo /WimFile:\"{filePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return null;

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    details.Images = ParseDismOutput(output);
                    return details;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DISM Parse Error: {ex.Message}");
                    return null;
                }
            });
        }

        private List<WimImageInfo> ParseDismOutput(string output)
        {
            var images = new List<WimImageInfo>();
            
            // DISM output sections are usually separated by blank lines or headers
            // We search for "Index : " pattern
            var sections = Regex.Split(output, @"(?=Index\s*:\s*\d+)");

            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section) || !section.Contains("Index")) continue;

                var info = new WimImageInfo();
                
                info.Index = ParseIntField(section, "Index");
                info.Name = ParseStringField(section, "Name");
                info.Description = ParseStringField(section, "Description");
                info.Size = ParseStringField(section, "Size");
                info.Architecture = ParseStringField(section, "Architecture");
                info.Version = ParseStringField(section, "Version");
                
                if (info.Index > 0)
                {
                    images.Add(info);
                }
            }

            return images;
        }

        private string ParseStringField(string input, string fieldName)
        {
            var match = Regex.Match(input, $@"{fieldName}\s*:\s*(.*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "N/A";
        }

        private int ParseIntField(string input, string fieldName)
        {
            var val = ParseStringField(input, fieldName);
            if (int.TryParse(val, out int result)) return result;
            return 0;
        }
    }
}

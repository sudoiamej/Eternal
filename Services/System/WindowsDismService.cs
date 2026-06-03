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

        public async Task<bool> InjectDriversAsync(string imagePath, string driverPath, bool forceUnsigned, Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string targetArg = string.Equals(imagePath, "Online", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(imagePath)
                        ? "/Online"
                        : $"/Image:\"{imagePath}\"";

                    string unsignedArg = forceUnsigned ? " /ForceUnsigned" : "";
                    string arguments = $"{targetArg} /Add-Driver /Driver:\"{driverPath}\" /Recurse{unsignedArg}";

                    progressCallback?.Invoke($"Executing: dism.exe {arguments}");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = psi };
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke($"ERROR: {e.Data}"); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Exception during driver injection: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> RestoreHealthFromSourceAsync(string targetPath, string sourceWimPath, int imageIndex, bool isOnline, Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string targetArg = isOnline ? "/Online" : $"/Image:\"{targetPath}\"";
                    string arguments = $"{targetArg} /Cleanup-Image /RestoreHealth /Source:wim:\"{sourceWimPath}\":{imageIndex} /LimitAccess";

                    progressCallback?.Invoke($"Executing: dism.exe {arguments}");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = psi };
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke(e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke($"ERROR: {e.Data}"); };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Exception during image restore health: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ApplyImageAsync(string sourceWimPath, int imageIndex, string targetDrive, Action<string> progressCallback)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 1. Apply image
                    string arguments = $"/Apply-Image /ImageFile:\"{sourceWimPath}\" /Index:{imageIndex} /ApplyDir:\"{targetDrive}\"";
                    progressCallback?.Invoke($"Flashing OS Files: dism.exe {arguments}");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = new Process { StartInfo = psi })
                    {
                        process.OutputDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke(e.Data); };
                        process.ErrorDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke($"ERROR: {e.Data}"); };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            progressCallback?.Invoke($"Flashing failed with exit code {process.ExitCode}");
                            return false;
                        }
                    }

                    // 2. Configure boot files using bcdboot.exe
                    progressCallback?.Invoke($"Configuring bootloader on target partition {targetDrive}...");
                    string targetRoot = targetDrive.TrimEnd('\\');
                    string bcdArguments = $"\"{targetRoot}\\Windows\" /s {targetRoot} /f ALL";
                    progressCallback?.Invoke($"Running: bcdboot.exe {bcdArguments}");

                    var bcdPsi = new ProcessStartInfo
                    {
                        FileName = "bcdboot.exe",
                        Arguments = bcdArguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var bcdProcess = new Process { StartInfo = bcdPsi })
                    {
                        bcdProcess.OutputDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke(e.Data); };
                        bcdProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) progressCallback?.Invoke($"ERROR: {e.Data}"); };

                        bcdProcess.Start();
                        bcdProcess.BeginOutputReadLine();
                        bcdProcess.BeginErrorReadLine();
                        bcdProcess.WaitForExit();

                        if (bcdProcess.ExitCode != 0)
                        {
                            progressCallback?.Invoke($"Boot configuration (bcdboot) returned non-zero code {bcdProcess.ExitCode}. Boot configuration may be incomplete depending on drive configuration.");
                        }
                    }

                    progressCallback?.Invoke("Flashing completed successfully!");
                    return true;
                }
                catch (Exception ex)
                {
                    progressCallback?.Invoke($"Exception during image flashing: {ex.Message}");
                    return false;
                }
            });
        }
    }
}

using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public class WindowsWinSatService : IWinSatService
    {
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query) 
            => new ManagementObjectSearcher(null, query, _wmiOptions);

        public async Task<WinSatScore?> GetCurrentScoresAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var searcher = CreateSearcher("select * from Win32_WinSAT");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return new WinSatScore
                        {
                            CpuScore = global::System.Convert.ToDouble(obj["CPUScore"] ?? 0),
                            MemoryScore = global::System.Convert.ToDouble(obj["MemoryScore"] ?? 0),
                            DiskScore = global::System.Convert.ToDouble(obj["DiskScore"] ?? 0),
                            GraphicsScore = global::System.Convert.ToDouble(obj["GraphicsScore"] ?? 0),
                            D3DScore = global::System.Convert.ToDouble(obj["D3DScore"] ?? 0),
                            BaseScore = global::System.Convert.ToDouble(obj["WinSPRLevel"] ?? 0),
                            AssessmentDate = obj["TimeTaken"]?.ToString() ?? "Unknown"
                        };
                    }
                }
                catch { }
                return null;
            });
        }

        public async Task<(bool Success, string Message)> RunAssessmentAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("winsat", "formal")
                    {
                        Verb = "runas", // Requires Admin
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal // Let user see progress in console
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit();

                    if (process?.ExitCode == 0)
                        return (true, "Assessment completed successfully.");
                    else
                        return (false, $"Assessment failed with exit code: {process?.ExitCode}");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });
        }
    }
}

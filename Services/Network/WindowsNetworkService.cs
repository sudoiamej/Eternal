using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Network
{
    public class WindowsNetworkService : INetworkService
    {
        private readonly Dictionary<string, (PerformanceCounter In, PerformanceCounter Out)> _counterCache = new();

        public Task<List<NetworkConnection>> GetActiveConnectionsAsync()
        {
            return Task.Run(() =>
            {
                var connections = new List<NetworkConnection>();
                var processCache = Process.GetProcesses().ToDictionary(p => p.Id, p => p.ProcessName);

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process == null) return connections;

                    using var reader = process.StandardOutput;
                    
                    // Skip headers (typically 4 lines)
                    for (int i = 0; i < 4 && !reader.EndOfStream; i++) reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        string[] parts = global::System.Text.RegularExpressions.Regex.Split(line, @"\s+");
                        if (parts.Length >= 4)
                        {
                            try 
                            {
                                string protocol = parts[0];
                                string local = parts[1];
                                string remote = parts[2];
                                string state = "UDP";
                                int pidIndex = parts.Length - 1;

                                if (parts.Length >= 5 && protocol.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                                {
                                    state = parts[3];
                                }

                                if (int.TryParse(parts[pidIndex], out int pid))
                                {
                                    processCache.TryGetValue(pid, out string procName);
                                    procName ??= "Unknown";
                                    connections.Add(new NetworkConnection(protocol, local, remote, state, pid, procName));
                                }
                            } catch { }
                        }
                    }
                }
                catch { }
                return connections;
            });
        }

        public Task<NetworkUsage> GetNetworkUsageAsync(string interfaceName)
        {
            return Task.Run(() =>
            {
                try
                {
                    string instanceName = GetPerformanceCounterInstanceName(interfaceName);
                    if (string.IsNullOrEmpty(instanceName)) return new NetworkUsage(0, 0);

                    if (!_counterCache.TryGetValue(instanceName, out var counters))
                    {
                        var category = new PerformanceCounterCategory("Network Interface");
                        var instances = category.GetInstanceNames();
                        
                        // Find closest match because WMI names and Performance Counter names differ slightly (e.g. #2, #3 suffix)
                        string bestMatch = instances.FirstOrDefault(i => i.Equals(instanceName, StringComparison.OrdinalIgnoreCase))
                                         ?? instances.FirstOrDefault(i => instanceName.Contains(i, StringComparison.OrdinalIgnoreCase) || i.Contains(instanceName, StringComparison.OrdinalIgnoreCase));

                        if (bestMatch != null)
                        {
                            counters = (
                                new PerformanceCounter("Network Interface", "Bytes Received/sec", bestMatch),
                                new PerformanceCounter("Network Interface", "Bytes Sent/sec", bestMatch)
                            );
                            _counterCache[instanceName] = counters;
                        }
                        else return new NetworkUsage(0, 0);
                    }

                    double received = counters.In.NextValue();
                    double sent = counters.Out.NextValue();

                    // Convert Bytes/sec to Mbps (bits per second / 1,000,000)
                    double downloadMbps = (received * 8) / 1_000_000.0;
                    double uploadMbps = (sent * 8) / 1_000_000.0;

                    return new NetworkUsage(downloadMbps, uploadMbps);
                }
                catch
                {
                    return new NetworkUsage(0, 0);
                }
            });
        }

        private string GetPerformanceCounterInstanceName(string interfaceName)
        {
            // Performance counters replace some characters like ( ) / with _
            return interfaceName
                .Replace("(", "[")
                .Replace(")", "]")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace("#", "_");
        }
    }
}
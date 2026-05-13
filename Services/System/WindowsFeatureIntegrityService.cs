using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Eternal.Services.Hardware;
using Eternal.Services.Storage;
using Eternal.Services.Network;

namespace Eternal.Services.System
{
    public class WindowsFeatureIntegrityService : IFeatureIntegrityService
    {
        private readonly IHardwareService _hardwareService;
        private readonly IStorageService _storageService;
        private readonly INetworkService _networkService;
        private readonly IProcessService _processService;

        public WindowsFeatureIntegrityService(
            IHardwareService hardwareService,
            IStorageService storageService,
            INetworkService networkService,
            IProcessService processService)
        {
            _hardwareService = hardwareService;
            _storageService = storageService;
            _networkService = networkService;
            _processService = processService;
        }

        public async Task<List<IntegrityResult>> RunFullDiagnosticAsync()
        {
            var results = new List<IntegrityResult>();

            // 1. Hardware Service Test
            results.Add(await TestFeatureAsync("Hardware Telemetry (WMI)", async () => {
                var info = await _hardwareService.GetCpuInfoAsync();
                if (string.IsNullOrEmpty(info.Name)) throw new Exception("Processor data missing");
            }));

            // 2. Storage Service Test
            results.Add(await TestFeatureAsync("Storage Architecture", async () => {
                var disks = await _storageService.GetPhysicalDisksAsync();
                if (disks.Count == 0) throw new Exception("No physical disks detected");
            }));

            // 3. Network Service Test
            results.Add(await TestFeatureAsync("Network Stack", async () => {
                var interfaces = await _networkService.GetActiveConnectionsAsync();
                // Even if no connections, the call should succeed
            }));

            // 4. Process Service Test
            results.Add(await TestFeatureAsync("Process Intelligence", async () => {
                var procs = await _processService.GetRunningProcessesAsync();
                if (procs.Count < 5) throw new Exception("Suspiciously low process count");
            }));

            return results;
        }

        private async Task<IntegrityResult> TestFeatureAsync(string name, Func<Task> testAction)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await testAction();
                sw.Stop();
                return new IntegrityResult { 
                    FeatureName = name, 
                    IsHealthy = true, 
                    Latency = sw.ElapsedMilliseconds + "ms" 
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new IntegrityResult { 
                    FeatureName = name, 
                    IsHealthy = false, 
                    Message = ex.Message, 
                    Latency = sw.ElapsedMilliseconds + "ms" 
                };
            }
        }
    }
}

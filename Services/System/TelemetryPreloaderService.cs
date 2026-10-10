using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Eternal.Services.Hardware;
using Eternal.Services.Network;
using Eternal.Services.Storage;

namespace Eternal.Services.System
{
    public interface ITelemetryPreloaderService
    {
        Task PreloadSystemTelemetryServicesAsync();
        bool IsPreloaded { get; }
    }

    public class TelemetryPreloaderService : ITelemetryPreloaderService
    {
        private readonly INetworkService _networkService;
        private readonly IHardwareService _hardwareService;
        private readonly IServicesService _servicesService;
        private readonly IProcessService _processService;
        private readonly IStorageService _storageService;
        private readonly IUserGroupService _userGroupService;

        public bool IsPreloaded { get; private set; } = false;

        public TelemetryPreloaderService(
            INetworkService networkService,
            IHardwareService hardwareService,
            IServicesService servicesService,
            IProcessService processService,
            IStorageService storageService,
            IUserGroupService userGroupService)
        {
            _networkService = networkService;
            _hardwareService = hardwareService;
            _servicesService = servicesService;
            _processService = processService;
            _storageService = storageService;
            _userGroupService = userGroupService;
        }

        public async Task PreloadSystemTelemetryServicesAsync()
        {
            var sw = Stopwatch.StartNew();
            Debug.WriteLine("[PRELOADER] Initiating Asynchronous Startup Telemetry Warm-up...");

            try
            {
                // Run background warm-up tasks concurrently
                var t1 = Task.Run(async () => { try { await _networkService.GetActiveConnectionsAsync(); } catch { } });
                var t2 = Task.Run(async () => { try { await _hardwareService.GetDetailedSystemInfoAsync(); } catch { } });
                var t3 = Task.Run(async () => { try { await _servicesService.GetServicesAsync(); } catch { } });
                var t4 = Task.Run(async () => { try { await _processService.GetRunningProcessesAsync(); } catch { } });
                var t5 = Task.Run(async () => { try { await _storageService.GetPhysicalDisksAsync(); } catch { } });
                var t6 = Task.Run(async () => { try { await _userGroupService.GetUsersAsync(); } catch { } });

                await Task.WhenAll(t1, t2, t3, t4, t5, t6);

                IsPreloaded = true;
                sw.Stop();
                Debug.WriteLine($"[PRELOADER] Startup Telemetry Preloaded in {sw.ElapsedMilliseconds}ms! Zero-wait tabs enabled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PRELOADER] Warm-up Error: {ex.Message}");
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Eternal.Helpers
{
    public static class ControlCenterSystemHelper
    {
        #region Win32 Native Imports
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool LockWorkStation();

        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern uint DnsFlushResolverCache();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        #endregion

        #region Silent Network & DNS Flush
        public static bool FlushDnsAndSocketsSilent()
        {
            try
            {
                // 1. Direct Win32 API Call (Zero windows, zero process spawns)
                uint res = DnsFlushResolverCache();
                if (res == 0) return true;

                // 2. Fallback: Silent hidden process execution
                var psi = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(1000);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DNS Flush Error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Thermal Turbo / CPU Unpark & High Priority Policy
        public static void SetThermalTurboState(bool active)
        {
            try
            {
                // Switch Windows Power Scheme silently to High Performance or Balanced
                string guid = active ? "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" : "381b4222-f694-41f0-9685-ff5bb260df2e";
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {guid}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(500);

                // Elevate current process priority class
                using var curProc = Process.GetCurrentProcess();
                curProc.PriorityClass = active ? ProcessPriorityClass.High : ProcessPriorityClass.Normal;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Thermal Turbo Error: {ex.Message}");
            }
        }
        #endregion

        #region Keep-Awake / Caffeine Mode
        public static void SetKeepAwakeState(bool active)
        {
            try
            {
                if (active)
                {
                    SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"KeepAwake Error: {ex.Message}");
            }
        }
        #endregion

        #region Power Scheme Presets
        public static void SetPowerPreset(string preset)
        {
            try
            {
                string guid = preset switch
                {
                    "Eco" => "a1844140-35d6-4476-8745-69d642107108",      // Power Saver
                    "Gaming" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",   // High Performance
                    _ => "381b4222-f694-41f0-9685-ff5bb260df2e"          // Balanced
                };

                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {guid}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(500);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Powercfg Error: {ex.Message}");
            }
        }
        #endregion
    }
}

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

        [DllImport("gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }
        #endregion

        #region Wi-Fi Control
        public static bool IsWifiEnabled()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT NetConnectionStatus FROM Win32_NetworkAdapter WHERE NetConnectionID LIKE '%Wi-Fi%' OR NetConnectionID LIKE '%Wireless%'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    ushort status = Convert.ToUInt16(obj["NetConnectionStatus"]);
                    return status == 2; // 2 = Connected, 9 = Enabled
                }
            }
            catch { }
            return true;
        }

        public static void SetWifiState(bool enable)
        {
            try
            {
                string action = enable ? "enable" : "disable";
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface set interface name=\"Wi-Fi\" admin={action}",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas" // Elevate admin
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Wi-Fi Control Error: {ex.Message}");
            }
        }
        #endregion

        #region Bluetooth Control
        public static bool IsBluetoothServiceRunning()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT State FROM Win32_Service WHERE Name='bthserv'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string state = obj["State"]?.ToString() ?? "";
                    return state.Equals("Running", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }

        public static void SetBluetoothState(bool enable)
        {
            try
            {
                string action = enable ? "start" : "stop";
                var psi = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = $"{action} bthserv",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Bluetooth Control Error: {ex.Message}");
            }
        }
        #endregion

        #region Do Not Disturb (Focus Assist / Toast Notification Suppressor)
        public static void SetDoNotDisturbState(bool active)
        {
            try
            {
                // Registry key for Windows Toast Notifications
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings");
                if (key != null)
                {
                    key.SetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED", active ? 0 : 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DND Error: {ex.Message}");
            }
        }
        #endregion

        #region Night Light (Gamma Ramp Filter)
        public static void SetNightLightState(bool active)
        {
            try
            {
                IntPtr hDC = GetDC(IntPtr.Zero);
                RAMP ramp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

                for (int i = 0; i < 256; i++)
                {
                    int value = i * 256;
                    ramp.Red[i] = (ushort)Math.Clamp(value, 0, 65535);
                    ramp.Green[i] = (ushort)Math.Clamp((int)(value * (active ? 0.85 : 1.0)), 0, 65535);
                    ramp.Blue[i] = (ushort)Math.Clamp((int)(value * (active ? 0.65 : 1.0)), 0, 65535); // Warm amber reduction
                }

                SetDeviceGammaRamp(hDC, ref ramp);
                ReleaseDC(IntPtr.Zero, hDC);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Night Light Error: {ex.Message}");
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
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Powercfg Error: {ex.Message}");
            }
        }
        #endregion
    }
}

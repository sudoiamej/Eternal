using System;
using System.Management;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Eternal.Helpers;

namespace Eternal.Services.System
{
    public class WindowsBiosService : IBiosService
    {
        private readonly EnumerationOptions _wmiOptions = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };

        private ManagementObjectSearcher CreateSearcher(string query, string? scope = null) 
            => new ManagementObjectSearcher(scope, query, _wmiOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetFirmwareEnvironmentVariable(string lpName, string lpGuid, IntPtr pBuffer, uint nSize);

        public Task<BiosInfo> GetBiosInfoAsync()
        {
            return Task.Run(() =>
            {
                string vendor = "Unknown";
                string version = "Unknown";
                string date = "Unknown";

                bool useNative = OsHelper.IsWindows11OrGreater();

                // 1. Primary: Registry Fallback (Native Registry - bypasses WMI)
                if (useNative)
                {
                    try
                    {
                        using var biosKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                        if (biosKey != null)
                        {
                            vendor = biosKey.GetValue("BIOSVendor")?.ToString() ?? vendor;
                            version = biosKey.GetValue("BIOSVersion")?.ToString() ?? version;
                            date = biosKey.GetValue("BIOSReleaseDate")?.ToString() ?? date;
                        }
                    }
                    catch { }
                }

                // 2. Secondary: WMI (Robust Fallback for Win10 or Registry issues)
                if (vendor == "Unknown" || version == "Unknown" || !useNative)
                {
                    try
                    {
                        using var searcher = CreateSearcher("select Manufacturer, SMBIOSBIOSVersion, ReleaseDate from Win32_BIOS");
                        using var collection = searcher.Get();
                        foreach (ManagementObject obj in collection)
                        {
                            using (obj)
                            {
                                vendor = obj["Manufacturer"]?.ToString() ?? vendor;
                                version = obj["SMBIOSBIOSVersion"]?.ToString() ?? version;
                                
                                // Only update date if it was still unknown or if we're explicitly favoring WMI formatting
                                string? wmiDate = obj["ReleaseDate"]?.ToString();
                                if (wmiDate != null && wmiDate.Length >= 8)
                                {
                                    date = $"{wmiDate.Substring(0, 4)}-{wmiDate.Substring(4, 2)}-{wmiDate.Substring(6, 2)}";
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"WMI BIOS Scan Error: {ex.Message}");
                    }
                }

                return new BiosInfo(vendor, version, date);
            });
        }

        public Task<UefiStatus> GetUefiStatusAsync()
        {
            return Task.Run(() =>
            {
                bool isUefi = false;
                bool secureBoot = false;
                TpmInfo tpm = new TpmInfo(false, "N/A", "None", "Not Detected", "N/A");

                try
                {
                    GetFirmwareEnvironmentVariable("", "{00000000-0000-0000-0000-000000000000}", IntPtr.Zero, 0);
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError != 1) isUefi = true;

                    if (!isUefi)
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control"))
                        {
                            var value = key?.GetValue("PEFirmwareType");
                            if (value != null) isUefi = global::System.Convert.ToInt32(value) == 2;
                        }
                    }

                    using (var key = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\SecureBoot\State"))
                    {
                        var value = key?.GetValue("UEFISecureBootEnabled");
                        if (value != null) secureBoot = global::System.Convert.ToInt32(value) == 1;
                    }

                    try
                    {
                        using var searcher = CreateSearcher("select IsEnabled_InitialValue, IsActivated_InitialValue, IsOwned_InitialValue, SpecVersion, ManufacturerId from Win32_Tpm", @"Root\CIMV2\Security\MicrosoftTpm");
                        using var collection = searcher.Get();
                        foreach (ManagementObject obj in collection)
                        {
                            using (obj)
                            {
                                bool isEnabled = global::System.Convert.ToBoolean(obj["IsEnabled_InitialValue"] ?? false);
                                bool isActive = global::System.Convert.ToBoolean(obj["IsActivated_InitialValue"] ?? false);
                                bool isOwned = global::System.Convert.ToBoolean(obj["IsOwned_InitialValue"] ?? false);
                                string specVersion = obj["SpecVersion"]?.ToString() ?? "Unknown";
                                string manufacturer = obj["ManufacturerId"]?.ToString() ?? "Unknown";
                                string status = isEnabled && isActive && isOwned ? "Ready for use" : (isEnabled ? "Enabled (Needs Ownership)" : "Inactive");
                                tpm = new TpmInfo(true, specVersion, GetTpmManufacturer(manufacturer), status, manufacturer);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"WMI TPM Scan Error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"UEFI Status Overall Scan Error: {ex.Message}");
                }
                return new UefiStatus(isUefi, secureBoot, tpm);
            });
        }

        private string GetTpmManufacturer(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Unknown";
            return id.Trim() switch
            {
                "414D4420" => "AMD",
                "41544D4C" => "Atmel",
                "4252434D" => "Broadcom",
                "49424D20" => "IBM",
                "49465800" => "Infineon",
                "494E5443" => "Intel",
                "4C454E00" => "Lenovo",
                "4D534654" => "Microsoft",
                "4E534D20" => "National Semi",
                "4E545A00" => "Nuvoton",
                "53544D20" => "ST Micro",
                "54584E00" => "Texas Instruments",
                "57424543" => "Winbond",
                "524F4343" => "Fuzhou Rockchip",
                "474F4F47" => "Google",
                _ => $"Unknown ({id})"
            };
        }
    }
}
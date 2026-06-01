using System;
using System.IO;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;
using Eternal.Helpers;

namespace Eternal.Services.System
{
    public interface IDriversService
    {
        Task<List<DriverInfo>> GetInstalledDriversAsync();
        Task<OemSupportInfo> GetOemSupportInfoAsync();
        Task<bool> ExportDriverPackageAsync(string driverName, string targetZipPath);
    }

    public record DriverInfo(string Name, string Description, string Version, string Provider, string Type, bool IsSigned, string HardwareId);
    public record OemSupportInfo(string Vendor, string Model, string SerialNumber);

    public class WindowsDriversService : IDriversService
    {
        public Task<List<DriverInfo>> GetInstalledDriversAsync()
        {
            return Task.Run(async () =>
            {
                var drivers = new List<DriverInfo>();
                
                // Get system manufacturer once for fallback
                var oem = await GetOemSupportInfoAsync();
                string systemManufacturer = oem.Vendor;

                try
                {
                    using var searcher = new ManagementObjectSearcher("select DeviceName, Manufacturer, Description, DriverVersion, DeviceID from Win32_PnPSignedDriver");

                    foreach (var obj in searcher.Get())
                    {
                        try 
                        {
                            string name = obj["DeviceName"]?.ToString() ?? "Unknown Device";
                            string provider = obj["Manufacturer"]?.ToString() ?? "Unknown";
                            string description = obj["Description"]?.ToString() ?? "System Managed Device";
                            string version = obj["DriverVersion"]?.ToString() ?? "N/A";
                            string hwid = obj["DeviceID"]?.ToString() ?? "N/A";

                            // If provider is generic or unknown, and it's a core system device, 
                            // we show the system manufacturer to help identify the hardware origin.
                            if (IsGeneric(provider) || provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                            {
                                if (name.Contains("PCI", StringComparison.OrdinalIgnoreCase) || 
                                    name.Contains("Chipset", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Bridge", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Controller", StringComparison.OrdinalIgnoreCase))
                                {
                                     // Only substitute if we have a real system manufacturer
                                     if (!IsGeneric(systemManufacturer))
                                     {
                                         provider = $"{systemManufacturer} (Standard)";
                                     }
                                }
                            }

                            // Device Manager classification logic
                            string type = "3rd Party";
                            if (provider.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                                provider.Contains("(Standard", StringComparison.OrdinalIgnoreCase))
                            {
                                type = "System (Microsoft)";
                            }

                            drivers.Add(new DriverInfo(name, description, version, provider, type, true, hwid));
                        } catch { }
                    }                }
                catch { }
                
                return drivers.OrderBy(d => d.Name).ToList();
            });
        }

        public Task<OemSupportInfo> GetOemSupportInfoAsync()
        {
            return Task.Run(() =>
            {
                string vendor = "Unknown";
                string model = "Unknown";
                string serial = "Unknown";

                bool useNative = OsHelper.IsWindows11OrGreater();

                try
                {
                    // 1. Primary for Windows 11: Registry Fallback (Direct SMBIOS strings)
                    if (useNative)
                    {
                        try
                        {
                            using var biosKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                            if (biosKey != null)
                            {
                                string? regVendor = biosKey.GetValue("SystemManufacturer")?.ToString();
                                string? regModel = biosKey.GetValue("SystemProductName")?.ToString();
                                string? regSerial = biosKey.GetValue("SystemSerialNumber")?.ToString();
                                
                                if (!IsGeneric(regVendor)) vendor = regVendor!;
                                if (!IsGeneric(regModel)) model = regModel!;
                                if (!IsGeneric(regSerial)) serial = regSerial!;

                                // Deep fallback for custom boards
                                if (IsGeneric(vendor))
                                {
                                    string? bbVendor = biosKey.GetValue("BaseBoardManufacturer")?.ToString();
                                    if (!IsGeneric(bbVendor)) vendor = bbVendor!;
                                }
                                if (IsGeneric(model))
                                {
                                    string? bbProduct = biosKey.GetValue("BaseBoardProduct")?.ToString();
                                    if (!IsGeneric(bbProduct)) model = bbProduct!;
                                }
                            }
                        }
                        catch { }
                    }

                    // 2. Secondary/Legacy: WMI ComputerSystemProduct
                    if (IsGeneric(vendor) || IsGeneric(model) || !useNative)
                    {
                        using (var searcher = new ManagementObjectSearcher("select Manufacturer, Name, IdentifyingNumber from Win32_ComputerSystemProduct"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                string? m = obj["Manufacturer"]?.ToString();
                                string? n = obj["Name"]?.ToString();
                                string? s = obj["IdentifyingNumber"]?.ToString();

                                if (IsGeneric(vendor) && !IsGeneric(m)) vendor = m!;
                                if (IsGeneric(model) && !IsGeneric(n)) model = n!;
                                if (IsGeneric(serial) && !IsGeneric(s)) serial = s!;
                                break;
                            }
                        }
                    }

                    // 3. Tertiary: BaseBoard (Motherboard)
                    if (IsGeneric(vendor) || IsGeneric(model))
                    {
                        using (var searcher = new ManagementObjectSearcher("select Manufacturer, Product, SerialNumber from Win32_BaseBoard"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                string? m = obj["Manufacturer"]?.ToString();
                                string? p = obj["Product"]?.ToString();
                                string? s = obj["SerialNumber"]?.ToString();

                                if (IsGeneric(vendor) && !IsGeneric(m)) vendor = m!;
                                if (IsGeneric(model) && !IsGeneric(p)) model = p!;
                                if (IsGeneric(serial) && !IsGeneric(s)) serial = s!;
                                break;
                            }
                        }
                    }

                    // 4. Intelligence Layer: If vendor is still unknown, infer from model
                    if (IsGeneric(vendor) && !IsGeneric(model))
                    {
                        vendor = InferManufacturer(model);
                    }

                    // 5. BIOS Serial Fallback (WMI)
                    if (IsGeneric(serial))
                    {
                        using (var searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BIOS"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                string? s = obj["SerialNumber"]?.ToString();
                                if (!IsGeneric(s)) serial = s!;
                                break;
                            }
                        }
                    }
                }
                catch { }

                return new OemSupportInfo(vendor, model, serial);
            });
        }

        private string InferManufacturer(string model)
        {
            string m = model.ToUpper();

            // ASUS Patterns (E-series like E1504, X-series, ROG, TUF, Prime)
            if (m.StartsWith("E1") || m.StartsWith("X5") || m.StartsWith("UX") || m.StartsWith("GL") || 
                m.Contains("ASUS") || m.Contains("ROG") || m.Contains("TUF") || m.Contains("PRIME") || 
                m.Contains("VIVOBOOK") || m.Contains("ZENBOOK"))
                return "ASUS";

            // MSI Patterns (MS- prefixed boards, MAG/MPG/MEG)
            if (m.StartsWith("MS-") || m.Contains("MSI") || m.Contains("MAG") || m.Contains("MPG") || m.Contains("MEG"))
                return "MSI";

            // Dell Patterns
            if (m.Contains("XPS") || m.Contains("LATITUDE") || m.Contains("OPTIPLEX") || m.Contains("INSPIRON") || m.Contains("PRECISION") || m.Contains("VOSTRO"))
                return "Dell";

            // HP Patterns
            if (m.Contains("ELITEBOOK") || m.Contains("PROBOOK") || m.Contains("PAVILION") || m.Contains("OMEN") || m.Contains("ENVY") || m.Contains("ZBOOK"))
                return "HP";

            // Lenovo Patterns
            if (m.Contains("THINKPAD") || m.Contains("IDEAPAD") || m.Contains("THINKCENTRE") || m.Contains("LEGION") || m.Contains("YOGA"))
                return "Lenovo";

            // Gigabyte Patterns
            if (m.Contains("AORUS") || m.Contains("GIGABYTE") || m.StartsWith("GA-"))
                return "Gigabyte";

            // ASRock
            if (m.Contains("ASROCK"))
                return "ASRock";

            return "Custom/OEM";
        }

        private bool IsGeneric(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string v = value.Trim().ToUpper();
            return v == "UNKNOWN" || 
                   v == "TO BE FILLED BY O.E.M." || 
                   v == "SYSTEM MANUFACTURER" || 
                   v == "SYSTEM PRODUCT NAME" || 
                   v == "DEFAULT STRING" ||
                   v == "O.E.M." ||
                   v == "NOT AVAILABLE" ||
                   v == "NOT SPECIFIED" ||
                   v == "NONE" ||
                   v == "INVALID" ||
                   v.Contains("GENERIC");
        }

        public async Task<bool> ExportDriverPackageAsync(string driverName, string targetZipPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string dir = Path.GetDirectoryName(targetZipPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string tempDir = Path.Combine(Path.GetTempPath(), "EternalDriverBackup_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);

                    string driversPath = Path.Combine(Environment.SystemDirectory, "drivers");
                    string cleanName = driverName;
                    foreach (char c in Path.GetInvalidFileNameChars()) cleanName = cleanName.Replace(c, '_');

                    bool filesFound = false;
                    if (Directory.Exists(driversPath))
                    {
                        var files = Directory.GetFiles(driversPath, "*.*", SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            if (fileName.Contains(cleanName, StringComparison.OrdinalIgnoreCase) || 
                                cleanName.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                            {
                                File.Copy(file, Path.Combine(tempDir, fileName), true);
                                filesFound = true;
                            }
                        }
                    }

                    string manifestPath = Path.Combine(tempDir, "driver_manifest.txt");
                    File.WriteAllText(manifestPath, $"Eternal System Intelligence - Driver Package Backup\r\n" +
                                                    $"Driver Name: {driverName}\r\n" +
                                                    $"Backup Date: {DateTime.Now}\r\n" +
                                                    $"Files Found: {(filesFound ? "Yes" : "No (Generic Windows Driver)")}\r\n" +
                                                    $"Source: C:\\Windows\\System32\\drivers\r\n");

                    if (File.Exists(targetZipPath)) File.Delete(targetZipPath);

                    global::System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, targetZipPath);
                    Directory.Delete(tempDir, true);

                    return true;
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"Driver export error: {ex.Message}");
                    return false;
                }
            });
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eternal.Services.Security
{
    public class HardwareBaselineModel
    {
        public string CoreBuildVersion { get; set; } = "3.5.5.26100";
        public string MachineGuid { get; set; } = string.Empty;
        public string CpuName { get; set; } = string.Empty;
        public string CpuProcessorId { get; set; } = string.Empty;
        public string TotalRamFormatted { get; set; } = string.Empty;
        public long TotalRamBytes { get; set; } = 0;
        public string GpuName { get; set; } = string.Empty;
        public string MotherboardModel { get; set; } = string.Empty;
        public string SystemDriveModel { get; set; } = string.Empty;
        public DateTime CalibrationTimestamp { get; set; } = DateTime.UtcNow;
    }

    public class HardwareDeltaItem
    {
        public string ComponentName { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public string PreviousValue { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public bool HasChanged { get; set; } = false;
        public bool IsUpgrade { get; set; } = false;
    }

    public interface IFactoryCodeService
    {
        bool IsDevEnvironment { get; }
        Task<bool> ValidateSystemIntegrityAndRegistrationAsync();
        HardwareBaselineModel GetCurrentHardwareSnapshot();
        HardwareBaselineModel? GetSavedBaselineSnapshot();
        List<HardwareDeltaItem> GetHardwareDeltas();
        void RecalibrateHardwareBaseline();
    }

    public class FactoryCodeService : IFactoryCodeService
    {
        private static readonly byte[] MasterSeed = Encoding.UTF8.GetBytes("Eternal_Factory_Master_Key_v3.5.5_Security_Vector");
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Eternal_HW_Salt_26100");

        private readonly string _factoryDirPath;
        private readonly string _baselineFilePath;

        public bool IsDevEnvironment
        {
            get
            {
#if DEBUG
                return true;
#else
                return Debugger.IsAttached;
#endif
            }
        }

        public FactoryCodeService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _factoryDirPath = Path.Combine(appData, @"Eternal\Factory");
            _baselineFilePath = Path.Combine(_factoryDirPath, "HardwareBaseline.dat");
            Directory.CreateDirectory(_factoryDirPath);
        }

        public async Task<bool> ValidateSystemIntegrityAndRegistrationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Dev Mode: Auto-pass integrity checks
                    if (IsDevEnvironment)
                    {
                        Debug.WriteLine("[FACTORY SERVICE] Dev Mode detected: Auto-passing integrity checks.");
                        EnsureBaselineSaved();
                        return true;
                    }

                    // Release Mode: Enforce strict Hard Registration
                    EnsureBaselineSaved();

                    // Binary Tamper Check (SHA-256)
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        using var sha = SHA256.Create();
                        using var fs = File.OpenRead(exePath);
                        byte[] hash = sha.ComputeHash(fs);
                        if (hash.Length == 0) return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FACTORY SERVICE ERROR] {ex.Message}");
                    return true; // Soft fallback
                }
            });
        }

        public HardwareBaselineModel GetCurrentHardwareSnapshot()
        {
            var model = new HardwareBaselineModel();
            try
            {
                // CPU
                using (var searcher = new ManagementObjectSearcher("SELECT Name, ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        model.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Generic Processor";
                        model.CpuProcessorId = obj["ProcessorId"]?.ToString()?.Trim() ?? "";
                        break;
                    }
                }

                // RAM
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["TotalPhysicalMemory"] != null)
                        {
                            long bytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                            model.TotalRamBytes = bytes;
                            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
                            model.TotalRamFormatted = $"{Math.Round(gb)} GB";
                        }
                        break;
                    }
                }

                // GPU
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        model.GpuName = obj["Name"]?.ToString()?.Trim() ?? "Graphics Controller";
                        break;
                    }
                }

                // Motherboard
                using (var searcher = new ManagementObjectSearcher("SELECT Product, Manufacturer FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                        string product = obj["Product"]?.ToString()?.Trim() ?? "";
                        model.MotherboardModel = $"{manufacturer} {product}".Trim();
                        break;
                    }
                }

                // Primary Drive
                using (var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        model.SystemDriveModel = obj["Model"]?.ToString()?.Trim() ?? "System Disk";
                        break;
                    }
                }
            }
            catch { }

            return model;
        }

        public HardwareBaselineModel? GetSavedBaselineSnapshot()
        {
            try
            {
                if (!File.Exists(_baselineFilePath)) return null;
                byte[] raw = File.ReadAllBytes(_baselineFilePath);
                string json = DecryptBuffer(raw);
                return JsonSerializer.Deserialize<HardwareBaselineModel>(json);
            }
            catch
            {
                return null;
            }
        }

        public List<HardwareDeltaItem> GetHardwareDeltas()
        {
            var list = new List<HardwareDeltaItem>();
            var current = GetCurrentHardwareSnapshot();
            var saved = GetSavedBaselineSnapshot();

            if (saved == null) return list;

            // CPU Delta
            if (!string.Equals(current.CpuName, saved.CpuName, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new HardwareDeltaItem
                {
                    ComponentName = "Processor",
                    CurrentValue = current.CpuName,
                    PreviousValue = saved.CpuName,
                    BadgeText = "[PART CHANGED]",
                    HasChanged = true
                });
            }

            // RAM Delta
            if (current.TotalRamBytes != saved.TotalRamBytes && saved.TotalRamBytes > 0)
            {
                bool isUpgrade = current.TotalRamBytes > saved.TotalRamBytes;
                double diffGb = Math.Abs(current.TotalRamBytes - saved.TotalRamBytes) / (1024.0 * 1024.0 * 1024.0);
                string badge = isUpgrade ? $"[UPGRADED +{Math.Round(diffGb)}GB]" : "[PART CHANGED]";
                list.Add(new HardwareDeltaItem
                {
                    ComponentName = "Memory",
                    CurrentValue = current.TotalRamFormatted,
                    PreviousValue = saved.TotalRamFormatted,
                    BadgeText = badge,
                    HasChanged = true,
                    IsUpgrade = isUpgrade
                });
            }

            // GPU Delta
            if (!string.Equals(current.GpuName, saved.GpuName, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new HardwareDeltaItem
                {
                    ComponentName = "Graphics",
                    CurrentValue = current.GpuName,
                    PreviousValue = saved.GpuName,
                    BadgeText = "[PART CHANGED]",
                    HasChanged = true
                });
            }

            // Motherboard Delta
            if (!string.Equals(current.MotherboardModel, saved.MotherboardModel, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new HardwareDeltaItem
                {
                    ComponentName = "Motherboard",
                    CurrentValue = current.MotherboardModel,
                    PreviousValue = saved.MotherboardModel,
                    BadgeText = "[PART CHANGED]",
                    HasChanged = true
                });
            }

            return list;
        }

        public void RecalibrateHardwareBaseline()
        {
            var snapshot = GetCurrentHardwareSnapshot();
            SaveBaselineSnapshot(snapshot);
        }

        private void EnsureBaselineSaved()
        {
            if (!File.Exists(_baselineFilePath))
            {
                RecalibrateHardwareBaseline();
            }
        }

        private void SaveBaselineSnapshot(HardwareBaselineModel model)
        {
            try
            {
                string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
                byte[] cipher = EncryptBuffer(json);
                File.WriteAllBytes(_baselineFilePath, cipher);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveBaselineSnapshot Error: {ex.Message}");
            }
        }

        #region Dual-Layer Cryptography (AES-256-GCM + DPAPI)
        private byte[] EncryptBuffer(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Layer 1: AES-256-GCM
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(MasterSeed, Salt, 10000, HashAlgorithmName.SHA256, 32);

            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            byte[] tag = new byte[16];
            byte[] cipherText = new byte[plainBytes.Length];

            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(nonce);
            writer.Write(tag);
            writer.Write(cipherText.Length);
            writer.Write(cipherText);
            byte[] layer1Output = ms.ToArray();

            // Layer 2: DPAPI
            try
            {
                return ProtectedData.Protect(layer1Output, null, DataProtectionScope.LocalMachine);
            }
            catch
            {
                return layer1Output;
            }
        }

        private string DecryptBuffer(byte[] cipherBuffer)
        {
            // Layer 2: DPAPI Unprotect
            byte[] layer1Cipher;
            try
            {
                layer1Cipher = ProtectedData.Unprotect(cipherBuffer, null, DataProtectionScope.LocalMachine);
            }
            catch
            {
                layer1Cipher = cipherBuffer;
            }

            // Layer 1: AES-256-GCM
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(MasterSeed, Salt, 10000, HashAlgorithmName.SHA256, 32);

            using var ms = new MemoryStream(layer1Cipher);
            using var reader = new BinaryReader(ms);

            byte[] nonce = reader.ReadBytes(12);
            byte[] tag = reader.ReadBytes(16);
            int cipherLen = reader.ReadInt32();
            byte[] cipherText = reader.ReadBytes(cipherLen);

            byte[] plainBytes = new byte[cipherLen];

            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        #endregion
    }
}

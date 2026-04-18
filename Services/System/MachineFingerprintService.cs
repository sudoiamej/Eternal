using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Eternal.Services.System
{
    public interface IMachineFingerprintService
    {
        string GetFingerprint();
    }

    public class MachineFingerprintService : IMachineFingerprintService
    {
        private string? _cachedFingerprint;

        public string GetFingerprint()
        {
            if (!string.IsNullOrEmpty(_cachedFingerprint)) return _cachedFingerprint;

            StringBuilder sb = new StringBuilder();

            // 1. Motherboard UUID (Most stable)
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (var obj in searcher.Get())
                {
                    sb.Append(obj["UUID"]?.ToString());
                }
            }
            catch { }

            // 2. CPU ID
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    sb.Append(obj["ProcessorId"]?.ToString());
                }
            }
            catch { }

            // 3. System Drive Serial
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index = 0");
                foreach (var obj in searcher.Get())
                {
                    sb.Append(obj["SerialNumber"]?.ToString());
                }
            }
            catch { }

            string rawData = sb.ToString();
            if (string.IsNullOrEmpty(rawData)) rawData = Environment.MachineName + Environment.UserName;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder hashBuilder = new StringBuilder();
                for (int i = 0; i < 8; i++) // Use first 8 bytes for a readable but unique ID (e.g., ETRN-XXXX-XXXX)
                {
                    hashBuilder.Append(bytes[i].ToString("X2"));
                    if (i == 3) hashBuilder.Append("-");
                }
                _cachedFingerprint = "ETRN-" + hashBuilder.ToString();
            }

            return _cachedFingerprint;
        }
    }
}

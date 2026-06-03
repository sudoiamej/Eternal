using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.Security
{
    public class WindowsFileForensicsService : IFileForensicsService
    {
        public async Task<FileForensicResult?> AnalyzeFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            return await Task.Run(() =>
            {
                try
                {
                    string md5 = CalculateHash(filePath, MD5.Create());
                    string sha256 = CalculateHash(filePath, SHA256.Create());
                    
                    string sigStatus = "Unsigned";
                    string signer = "N/A";
                    string issuer = "N/A";
                    DateTime? timestamp = null;
                    bool isTrusted = false;

                    try
                    {
                        // Modern approach to extract certificates from signed binaries
                        var cert2 = X509CertificateLoader.LoadCertificateFromFile(filePath);
                        signer = cert2.Subject;
                        issuer = cert2.Issuer;
                        sigStatus = "Signed";
                        
                        // Basic trust check
                        var chain = new X509Chain();
                        isTrusted = chain.Build(cert2);
                        if (isTrusted) sigStatus = "Signed & Trusted";
                        else sigStatus = "Signed (Untrusted)";
                    }
                    catch { }

                    double entropy = CalculateShannonEntropy(filePath);
                    var suspiciousApis = DetectSuspiciousApis(filePath);

                    return new FileForensicResult(
                        Path.GetFileName(filePath),
                        filePath,
                        md5,
                        sha256,
                        sigStatus,
                        signer,
                        issuer,
                        timestamp,
                        isTrusted,
                        entropy,
                        suspiciousApis
                    );
                }
                catch { return null; }
            });
        }

        private string CalculateHash(string path, HashAlgorithm algorithm)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] hash = algorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private double CalculateShannonEntropy(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = Math.Min(stream.Length, 10 * 1024 * 1024); // Limit to 10MB
                    byte[] buffer = new byte[length];
                    int read = stream.Read(buffer, 0, (int)length);
                    if (read == 0) return 0;
                    
                    int[] counts = new int[256];
                    for (int i = 0; i < read; i++)
                    {
                        counts[buffer[i]]++;
                    }
                    
                    double entropy = 0;
                    double total = read;
                    for (int i = 0; i < 256; i++)
                    {
                        if (counts[i] > 0)
                        {
                            double p = counts[i] / total;
                            entropy -= p * Math.Log(p, 2);
                        }
                    }
                    return entropy;
                }
            }
            catch
            {
                return 0;
            }
        }

        private List<string> DetectSuspiciousApis(string filePath)
        {
            var detected = new List<string>();
            var apis = new[] { "VirtualAlloc", "WriteProcessMemory", "CreateRemoteThread", "VirtualProtect", "QueueUserAPC", "SetThreadContext" };
            if (!File.Exists(filePath)) return detected;
            
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = Math.Min(stream.Length, 5 * 1024 * 1024); // Scan first 5MB
                    byte[] buffer = new byte[length];
                    int read = stream.Read(buffer, 0, (int)length);
                    
                    string content = global::System.Text.Encoding.ASCII.GetString(buffer, 0, read);
                    foreach (var api in apis)
                    {
                        if (content.Contains(api, StringComparison.OrdinalIgnoreCase))
                        {
                            detected.Add(api);
                        }
                    }
                }
            }
            catch { }
            return detected;
        }
    }
}

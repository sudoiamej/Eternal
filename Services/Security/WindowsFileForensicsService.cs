using System;
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
                        var cert2 = new X509Certificate2(filePath);
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

                    return new FileForensicResult(
                        Path.GetFileName(filePath),
                        filePath,
                        md5,
                        sha256,
                        sigStatus,
                        signer,
                        issuer,
                        timestamp,
                        isTrusted
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
    }
}

using System;
using System.Collections.Generic;

namespace Eternal.Models
{
    public record FileForensicResult(
        string FileName,
        string FilePath,
        string Md5Hash,
        string Sha256Hash,
        string SignatureStatus,
        string SignerName,
        string Issuer,
        DateTime? Timestamp,
        bool IsTrusted,
        double Entropy,
        List<string> SuspiciousApis
    );
}

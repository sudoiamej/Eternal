using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IPcScannerService
    {
        Task<List<ScannerIssue>> RunFullScanAsync(IProgress<int> progress);
        Task<bool> ExecuteFixAsync(ScannerIssue issue);
    }
}

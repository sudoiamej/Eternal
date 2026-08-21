using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Eternal.Helpers
{
    public static class AntiTamperHelper
    {
        /// <summary>
        /// Audits if the host system motherboard, BIOS, or video controller indicates a virtual machine sandbox.
        /// </summary>
        public static bool IsVirtualMachineDetected()
        {
            try
            {
                // Bypass VM check if in a visual studio debugging session
                if (AntiDebugHelper.IsDeveloperExceptionActive())
                    return false;

                // 1. Motherboard / BaseBoard Manufacturer Audit
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_BaseBoard"))
                using (var collection = searcher.Get())
                {
                    foreach (var obj in collection)
                    {
                        string manufacturer = obj["Manufacturer"]?.ToString().ToLower() ?? "";
                        string product = obj["Product"]?.ToString().ToLower() ?? "";
                        if (manufacturer.Contains("microsoft") || manufacturer.Contains("vmware") || 
                            manufacturer.Contains("virtualbox") || manufacturer.Contains("oracle") ||
                            product.Contains("virtual") || product.Contains("hyper-v"))
                        {
                            return true;
                        }
                    }
                }

                // 2. Video Controller Driver Audit
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_VideoController"))
                using (var collection = searcher.Get())
                {
                    foreach (var obj in collection)
                    {
                        string name = obj["Name"]?.ToString().ToLower() ?? "";
                        if (name.Contains("vmware") || name.Contains("virtualbox") || 
                            name.Contains("hyper-v") || name.Contains("qemu") || name.Contains("basic render"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Measures processor instruction execution time difference to detect hypervisor virtualization bottlenecks.
        /// </summary>
        public static bool VerifyExecutionTiming()
        {
            try
            {
                if (AntiDebugHelper.IsDeveloperExceptionActive())
                    return false;

                var watch = Stopwatch.StartNew();
                
                // Simple arithmetic iteration loop
                long sum = 0;
                for (int i = 0; i < 5000000; i++)
                {
                    sum += i;
                }
                
                watch.Stop();
                
                // On a normal physical processor, 5M iterations take less than 15ms.
                // In a hypervisor single-step trace, it takes significantly longer.
                if (watch.ElapsedMilliseconds > 150)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}

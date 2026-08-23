using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Eternal.Helpers
{
    public static class MemoryOptimizerHelper
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        /// <summary>
        /// Forces garbage collection, flushes Large Object Heap (LOH), and trims physical RAM working set.
        /// </summary>
        public static void OptimizeMemory()
        {
            try
            {
                // Step 1: Force full GC across all generations (0, 1, 2)
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

                // Step 2: Trim Win32 working set RAM footprint
                using var currentProcess = Process.GetCurrentProcess();
                SetProcessWorkingSetSize(currentProcess.Handle, new IntPtr(-1), new IntPtr(-1));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Memory Optimization Error: {ex.Message}");
            }
        }
    }
}

using System;
using System.Runtime.InteropServices;

namespace Eternal.Services.System
{
    public sealed class NativePdhEngine : IDisposable
    {
        private IntPtr _queryHandle = IntPtr.Zero;
        private IntPtr _cpuCounterHandle = IntPtr.Zero;
        private bool _disposed = false;

        public NativePdhEngine()
        {
            InitializeNativePdhQuery();
        }

        private void InitializeNativePdhQuery()
        {
            try
            {
                if (PdhOpenQuery(IntPtr.Zero, IntPtr.Zero, out _queryHandle) == 0)
                {
                    // Add Total Processor Time counter
                    PdhAddCounterW(_queryHandle, @"\ProcessorInformation(_Total)\% Processor Utility", IntPtr.Zero, out _cpuCounterHandle);
                    
                    if (_cpuCounterHandle == IntPtr.Zero)
                    {
                        // Fallback counter for older Win10 builds
                        PdhAddCounterW(_queryHandle, @"\Processor(_Total)\% Processor Time", IntPtr.Zero, out _cpuCounterHandle);
                    }

                    // Initial collection to prime PDH delta calculation
                    PdhCollectQueryData(_queryHandle);
                }
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"Native PDH Initialization Error: {ex.Message}");
            }
        }

        public double GetCpuUsageNative()
        {
            if (_queryHandle == IntPtr.Zero || _cpuCounterHandle == IntPtr.Zero) return 0.0;

            try
            {
                if (PdhCollectQueryData(_queryHandle) == 0)
                {
                    if (PdhGetFormattedCounterValue(_cpuCounterHandle, PDH_FMT_DOUBLE, out _, out PDH_FMT_COUNTERVALUE_DOUBLE val) == 0)
                    {
                        return Math.Max(0.0, Math.Min(100.0, val.doubleValue));
                    }
                }
            }
            catch { }

            return 0.0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_queryHandle != IntPtr.Zero)
                {
                    PdhCloseQuery(_queryHandle);
                    _queryHandle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        #region Native PDH P/Invoke

        private const uint PDH_FMT_DOUBLE = 0x00000200;

        [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint PdhOpenQuery(IntPtr szDataSource, IntPtr dwUserData, out IntPtr phQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint PdhAddCounterW(IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern uint PdhCollectQueryData(IntPtr hQuery);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern uint PdhGetFormattedCounterValue(IntPtr hCounter, uint dwFormat, out uint lpdwType, out PDH_FMT_COUNTERVALUE_DOUBLE pValue);

        [DllImport("pdh.dll", SetLastError = true)]
        private static extern uint PdhCloseQuery(IntPtr hQuery);

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_DOUBLE
        {
            public uint CStatus;
            public double doubleValue;
        }

        #endregion
    }
}

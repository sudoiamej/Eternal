using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Eternal.Services.System
{
    public interface ISmartIdleEngine
    {
        void InitializeIdleWatcher(Action<bool> onIdleStateChanged);
        void TrimWorkingSetMemory();
    }

    public class SmartIdleEngine : ISmartIdleEngine
    {
        private bool _isIdle = false;
        private const int IDLE_THRESHOLD_SECONDS = 30;

        public void InitializeIdleWatcher(Action<bool> onIdleStateChanged)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        uint idleTimeSec = GetIdleTimeSeconds();
                        bool nowIdle = idleTimeSec >= IDLE_THRESHOLD_SECONDS;

                        if (nowIdle != _isIdle)
                        {
                            _isIdle = nowIdle;
                            Debug.WriteLine($"[SMART IDLE] State Changed. IsIdle: {_isIdle}");

                            if (_isIdle)
                            {
                                TrimWorkingSetMemory();
                            }

                            onIdleStateChanged?.Invoke(_isIdle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SMART IDLE] Error: {ex.Message}");
                    }

                    await Task.Delay(2000);
                }
            });
        }

        public void TrimWorkingSetMemory()
        {
            try
            {
                Debug.WriteLine("[MEMORY PRESERVATION] Compacting Managed & Unmanaged Working Set...");
                GC.Collect(2, GCCollectionMode.Optimized, false);
                GC.WaitForPendingFinalizers();

                IntPtr hProcess = Process.GetCurrentProcess().Handle;
                SetProcessWorkingSetSize(hProcess, (IntPtr)(-1), (IntPtr)(-1));
                Debug.WriteLine("[MEMORY PRESERVATION] Working Set Trimmed to Minimum Page Limit.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MEMORY PRESERVATION] Trim Error: {ex.Message}");
            }
        }

        private static uint GetIdleTimeSeconds()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint envTicks = (uint)Environment.TickCount;
                return (envTicks - lastInputInfo.dwTime) / 1000;
            }

            return 0;
        }

        #region Native Win32 Input & Memory APIs

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        #endregion
    }
}

using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Eternal.Helpers
{
    public static class AntiDebugHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool pbDebuggerPresent);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            out IntPtr processInformation,
            int processInformationLength,
            out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess", SetLastError = true)]
        private static extern int NtQueryInformationProcessBasic(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, System.Text.StringBuilder lpExeName, ref uint lpdwSize);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        /// <summary>
        /// Detects if the application is running inside a Visual Studio debugging/host context.
        /// </summary>
        public static bool IsDeveloperExceptionActive()
        {
            // Factor 1: VS Specific Environment Variables
            bool envHasVs = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSAPPIDNAME")) || 
                            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("visualstudioedition")) ||
                            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSAPPIDDIR"));

            // Factor 2: Process Tree Parent Lookup (devenv / msbuild / dotnet)
            bool parentIsVs = false;
            try
            {
                var pbi = new PROCESS_BASIC_INFORMATION();
                int size = Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
                int status = NtQueryInformationProcessBasic(
                    Process.GetCurrentProcess().Handle,
                    0, // ProcessBasicInformation
                    ref pbi,
                    size,
                    out _);

                if (status == 0)
                {
                    int parentPid = pbi.InheritedFromUniqueProcessId.ToInt32();
                    if (parentPid > 0)
                    {
                        IntPtr hParent = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, parentPid);
                        if (hParent != IntPtr.Zero)
                        {
                            try
                            {
                                uint pathSize = 1024;
                                var sb = new System.Text.StringBuilder((int)pathSize);
                                if (QueryFullProcessImageName(hParent, 0, sb, ref pathSize))
                                {
                                    string parentPath = sb.ToString().ToLower();
                                    string parentName = System.IO.Path.GetFileName(parentPath);
                                    if (parentName.Contains("devenv") || parentName.Contains("msbuild") || parentName.Contains("dotnet"))
                                    {
                                        parentIsVs = true;
                                    }
                                }
                            }
                            finally
                            {
                                CloseHandle(hParent);
                            }
                        }
                    }
                }
            }
            catch { }

            return envHasVs || parentIsVs;
        }

        /// <summary>
        /// Audits the operating environment for active debugger attachments.
        /// Returns true if a debugger (and no developer exception) is detected.
        /// </summary>
        public static bool IsDebuggerDetected()
        {
            // If legitimate Visual Studio developer environment, immediately bypass
            if (IsDeveloperExceptionActive())
            {
                return false;
            }

            // Layer 1: CLR Managed Check
            if (Debugger.IsAttached)
            {
                return true;
            }

            // Layer 2: PEB BeingDebugged Check
            if (IsDebuggerPresent())
            {
                return true;
            }

            // Layer 3: Remote Debugger Verification
            bool remoteAttached = false;
            if (CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remoteAttached) && remoteAttached)
            {
                return true;
            }

            // Layer 4: NtQueryInformationProcess (ProcessDebugPort)
            IntPtr debugPort = IntPtr.Zero;
            int status = NtQueryInformationProcess(
                Process.GetCurrentProcess().Handle,
                7, // ProcessDebugPort
                out debugPort,
                Marshal.SizeOf(typeof(IntPtr)),
                out _);

            if (status == 0 && debugPort != IntPtr.Zero)
            {
                return true;
            }

            // Layer 5: Forensic Process Target List
            string[] suspiciousNames = { "dnspy", "x64dbg", "windbg", "ida64", "processhacker", "cheatengine" };
            try
            {
                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    string pName = p.ProcessName.ToLower();
                    foreach (var s in suspiciousNames)
                    {
                        if (pName.Contains(s))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }
}

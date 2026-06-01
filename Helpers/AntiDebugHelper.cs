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
                int myPid = Process.GetCurrentProcess().Id;
                using var searcher = new ManagementObjectSearcher($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {myPid}");
                using var collection = searcher.Get();
                foreach (ManagementObject obj in collection)
                {
                    int parentPid = Convert.ToInt32(obj["ParentProcessId"]);
                    using var parentProc = Process.GetProcessById(parentPid);
                    string parentName = parentProc.ProcessName.ToLower();

                    if (parentName.Contains("devenv") || parentName.Contains("msbuild") || parentName.Contains("dotnet"))
                    {
                        parentIsVs = true;
                        break;
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

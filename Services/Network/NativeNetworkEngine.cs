using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Eternal.Models;

namespace Eternal.Services.Network
{
    public static class NativeNetworkEngine
    {
        private const int AF_INET = 2; // IPv4
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const int UDP_TABLE_OWNER_PID = 1;

        public static List<NetworkConnection> GetActiveConnectionsNative()
        {
            var connections = new List<NetworkConnection>();
            var processCache = new Dictionary<int, string>();

            try
            {
                foreach (var proc in Process.GetProcesses())
                {
                    try { processCache[proc.Id] = proc.ProcessName; } catch { }
                }
            }
            catch { }

            // 1. Native IPv4 TCP Socket Enumeration (GetExtendedTcpTable)
            IntPtr tcpTablePtr = IntPtr.Zero;
            uint tcpBufferSize = 0;

            GetExtendedTcpTable(IntPtr.Zero, ref tcpBufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
            tcpTablePtr = Marshal.AllocHGlobal((int)tcpBufferSize);

            try
            {
                if (GetExtendedTcpTable(tcpTablePtr, ref tcpBufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0) == 0)
                {
                    int numEntries = Marshal.ReadInt32(tcpTablePtr);
                    IntPtr rowPtr = tcpTablePtr + 4;

                    for (int i = 0; i < numEntries; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                        string localIp = new IPAddress(row.dwLocalAddr).ToString();
                        int localPort = (ushort)IPAddress.NetworkToHostOrder((short)row.dwLocalPort);
                        string remoteIp = new IPAddress(row.dwRemoteAddr).ToString();
                        int remotePort = (ushort)IPAddress.NetworkToHostOrder((short)row.dwRemotePort);
                        string state = GetTcpStateString(row.dwState);

                        int pid = (int)row.dwOwningPid;
                        processCache.TryGetValue(pid, out string? procName);

                        connections.Add(new NetworkConnection(
                            "TCP",
                            $"{localIp}:{localPort}",
                            $"{remoteIp}:{remotePort}",
                            state,
                            pid,
                            procName ?? "Unknown"
                        ));

                        rowPtr += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                    }
                }
            }
            finally
            {
                if (tcpTablePtr != IntPtr.Zero) Marshal.FreeHGlobal(tcpTablePtr);
            }

            // 2. Native IPv4 UDP Socket Enumeration (GetExtendedUdpTable)
            IntPtr udpTablePtr = IntPtr.Zero;
            uint udpBufferSize = 0;

            GetExtendedUdpTable(IntPtr.Zero, ref udpBufferSize, true, AF_INET, UDP_TABLE_OWNER_PID, 0);
            udpTablePtr = Marshal.AllocHGlobal((int)udpBufferSize);

            try
            {
                if (GetExtendedUdpTable(udpTablePtr, ref udpBufferSize, true, AF_INET, UDP_TABLE_OWNER_PID, 0) == 0)
                {
                    int numEntries = Marshal.ReadInt32(udpTablePtr);
                    IntPtr rowPtr = udpTablePtr + 4;

                    for (int i = 0; i < numEntries; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);

                        string localIp = new IPAddress(row.dwLocalAddr).ToString();
                        int localPort = (ushort)IPAddress.NetworkToHostOrder((short)row.dwLocalPort);

                        int pid = (int)row.dwOwningPid;
                        processCache.TryGetValue(pid, out string? procName);

                        connections.Add(new NetworkConnection(
                            "UDP",
                            $"{localIp}:{localPort}",
                            "*:*",
                            "Listening",
                            pid,
                            procName ?? "Unknown"
                        ));

                        rowPtr += Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();
                    }
                }
            }
            finally
            {
                if (udpTablePtr != IntPtr.Zero) Marshal.FreeHGlobal(udpTablePtr);
            }

            return connections;
        }

        private static string GetTcpStateString(uint state) => state switch
        {
            1 => "CLOSED",
            2 => "LISTENING",
            3 => "SYN_SENT",
            4 => "SYN_RCVD",
            5 => "ESTABLISHED",
            6 => "FIN_WAIT1",
            7 => "FIN_WAIT2",
            8 => "CLOSE_WAIT",
            9 => "CLOSING",
            10 => "LAST_ACK",
            11 => "TIME_WAIT",
            12 => "DELETE_TCB",
            _ => "UNKNOWN"
        };

        #region Native IP Helper P/Invoke Structures

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref uint pdwSize,
            bool bOrder,
            int ulAf,
            int TableClass,
            uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable,
            ref uint pdwSize,
            bool bOrder,
            int ulAf,
            int TableClass,
            uint reserved);

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwOwningPid;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class DiskSectorViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<HexRowModel> _sectorRows = new();
        [ObservableProperty] private string _drivePath = @"\\.\PhysicalDrive0";
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _bootSignatureStatus = "0xAA55 Pending Inspection";
        [ObservableProperty] private string _partitionTableInfo = "Sector 0 (512 Bytes) Raw Hex";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        public DiskSectorViewModel()
        {
            _ = ReadSectorZeroAsync();
        }

        [RelayCommand]
        public async Task ReadSectorZeroAsync()
        {
            StatusText = $"Reading Sector 0 of {DrivePath}...";
            SectorRows.Clear();

            await Task.Run(() =>
            {
                byte[] buffer = new byte[512];
                bool success = false;
                string sigStatus = "Boot Signature: 0x0000 (Invalid)";

                IntPtr hDrive = CreateFile(
                    DrivePath,
                    GENERIC_READ,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (hDrive != IntPtr.Zero && hDrive != new IntPtr(-1))
                {
                    try
                    {
                        if (ReadFile(hDrive, buffer, 512, out uint bytesRead, IntPtr.Zero) && bytesRead == 512)
                        {
                            success = true;
                            if (buffer[510] == 0x55 && buffer[511] == 0xAA)
                            {
                                sigStatus = "Boot Signature: 0xAA55 (Valid MBR/GPT Partition Sector)";
                            }
                        }
                    }
                    finally
                    {
                        CloseHandle(hDrive);
                    }
                }

                var rows = new List<HexRowModel>();
                if (success)
                {
                    for (int i = 0; i < 512; i += 16)
                    {
                        string offsetStr = i.ToString("X8");
                        var hexParts = new List<string>();
                        var charParts = new List<char>();

                        for (int j = 0; j < 16; j++)
                        {
                            byte b = buffer[i + j];
                            hexParts.Add(b.ToString("X2"));
                            charParts.Add(b >= 32 && b <= 126 ? (char)b : '.');
                        }

                        rows.Add(new HexRowModel
                        {
                            Offset = offsetStr,
                            HexBytes = string.Join(" ", hexParts).PadRight(47),
                            AsciiString = new string(charParts.ToArray())
                        });
                    }
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    SectorRows = new ObservableCollection<HexRowModel>(rows);
                    BootSignatureStatus = sigStatus;
                    StatusText = success ? $"Sector 0 read successfully from {DrivePath}" : $"Elevated Administrator privileges required to read raw sector on {DrivePath}";
                });
            });
        }
    }
}

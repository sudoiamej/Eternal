using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Eternal.Models;

namespace Eternal.Services.Hardware
{
    public class WindowsDisplayService : IDisplayService
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const uint CDS_UPDATEREGISTRY = 0x01;
        private const uint CDS_TEST = 0x02;

        public async Task<List<MonitorInfo>> GetMonitorsAsync()
        {
            return await Task.Run(() =>
            {
                var monitorList = new List<MonitorInfo>();
                var screens = Screen.AllScreens;
                
                // Pre-fetch WMI Monitor IDs for friendly names
                var wmiMonitorNames = GetWmiMonitorNames();

                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    var info = new MonitorInfo
                    {
                        Index = i + 1,
                        Name = screen.DeviceName,
                        IsPrimary = screen.Primary,
                        CurrentWidth = screen.Bounds.Width,
                        CurrentHeight = screen.Bounds.Height,
                        Left = screen.Bounds.Left,
                        Top = screen.Bounds.Top,
                        Right = screen.Bounds.Right,
                        Bottom = screen.Bounds.Bottom,
                        Orientation = screen.Bounds.Width > screen.Bounds.Height ? "Landscape" : "Portrait"
                    };

                    // Deep Discovery via EnumDisplayDevices
                    var device = new DISPLAY_DEVICE();
                    device.cb = Marshal.SizeOf(device);
                    
                    // Step 1: Get Monitor Hardware Identity
                    if (EnumDisplayDevices(screen.DeviceName, 0, ref device, 0x1)) // EDD_GET_DEVICE_INTERFACE_NAME = 0x1
                    {
                        info.DeviceID = device.DeviceID; // Hardware ID (PnP ID)
                        info.Model = device.DeviceString; // Initial model string

                        // Step 2: Correlate with WMI for User-Friendly Name
                        if (wmiMonitorNames.TryGetValue(device.DeviceID.ToUpper(), out string? friendlyName))
                        {
                            info.Model = friendlyName;
                        }
                    }

                    // Fetch supported resolutions and refresh rates via EnumDisplaySettings
                    var devMode = new DEVMODE();
                    devMode.dmSize = (short)Marshal.SizeOf(devMode);
                    
                    int modeIndex = 0;
                    var resDict = new Dictionary<string, List<int>>();

                    while (EnumDisplaySettings(screen.DeviceName, modeIndex, ref devMode))
                    {
                        string key = $"{devMode.dmPelsWidth}x{devMode.dmPelsHeight}";
                        if (!resDict.ContainsKey(key)) resDict[key] = new List<int>();
                        
                        if (!resDict[key].Contains(devMode.dmDisplayFrequency))
                            resDict[key].Add(devMode.dmDisplayFrequency);

                        if (devMode.dmPelsWidth == screen.Bounds.Width && devMode.dmPelsHeight == screen.Bounds.Height)
                            info.RefreshRate = devMode.dmDisplayFrequency;

                        modeIndex++;
                    }

                    foreach (var res in resDict)
                    {
                        var parts = res.Key.Split('x');
                        info.SupportedResolutions.Add(new ResolutionPreset(
                            int.Parse(parts[0]), int.Parse(parts[1]), res.Value.OrderByDescending(f => f).ToList()));
                    }

                    monitorList.Add(info);
                }

                return monitorList;
            });
        }

        private Dictionary<string, string> GetWmiMonitorNames()
        {
            var results = new Dictionary<string, string>();
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorID");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string instanceName = obj["InstanceName"]?.ToString()?.ToUpper() ?? "";
                    if (string.IsNullOrEmpty(instanceName)) continue;

                    // Instance name in WMI usually has a suffix like _0, remove it for matching
                    int lastUnderscore = instanceName.LastIndexOf('_');
                    if (lastUnderscore > 0) instanceName = instanceName.Substring(0, lastUnderscore);

                    var nameBytes = obj["UserFriendlyName"] as ushort[];
                    if (nameBytes != null)
                    {
                        string name = "";
                        foreach (var b in nameBytes)
                        {
                            if (b == 0) break;
                            name += (char)b;
                        }
                        if (!string.IsNullOrEmpty(name))
                            results[instanceName] = name;
                    }
                }
            }
            catch { }
            return results;
        }

        public async Task<List<DisplayAdapter>> GetAdaptersAsync()
        {
            return await Task.Run(() =>
            {
                var adapters = new List<DisplayAdapter>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM, Status FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        adapters.Add(new DisplayAdapter
                        {
                            Name = obj["Name"]?.ToString() ?? "Unknown GPU",
                            DriverVersion = obj["DriverVersion"]?.ToString() ?? "N/A",
                            VramBytes = Convert.ToInt64(obj["AdapterRAM"] ?? 0),
                            Status = obj["Status"]?.ToString() ?? "OK"
                        });
                    }
                }
                catch { }
                return adapters;
            });
        }

        public async Task<bool> ApplyDisplaySettingsAsync(MonitorInfo monitor, int width, int height, int refreshRate)
        {
            return await Task.Run(() =>
            {
                var devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                devMode.dmPelsWidth = width;
                devMode.dmPelsHeight = height;
                devMode.dmDisplayFrequency = refreshRate;
                devMode.dmFields = 0x00080000 | 0x00100000 | 0x00400000; // PELSWIDTH | PELSHEIGHT | DISPLAYFREQUENCY

                int result = ChangeDisplaySettingsEx(monitor.Name, ref devMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
                return result == DISP_CHANGE_SUCCESSFUL;
            });
        }

        public async Task<bool> IdentifyMonitorAsync(MonitorInfo monitor)
        {
            return await Task.Run(() =>
            {
                global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var idWin = new global::System.Windows.Window
                    {
                        WindowStyle = global::System.Windows.WindowStyle.None,
                        AllowsTransparency = true,
                        Background = global::System.Windows.Media.Brushes.Transparent,
                        Topmost = true,
                        ShowInTaskbar = false,
                        Left = monitor.Left + (monitor.CurrentWidth / 2) - 100,
                        Top = monitor.Top + (monitor.CurrentHeight / 2) - 100,
                        Width = 200,
                        Height = 200,
                        Content = new global::System.Windows.Controls.Border
                        {
                            Background = new global::System.Windows.Media.SolidColorBrush(global::System.Windows.Media.Color.FromArgb(200, 0, 0, 0)),
                            CornerRadius = new global::System.Windows.CornerRadius(100),
                            Child = new global::System.Windows.Controls.TextBlock
                            {
                                Text = monitor.Index.ToString(),
                                Foreground = global::System.Windows.Media.Brushes.White,
                                FontSize = 120,
                                FontWeight = global::System.Windows.FontWeights.Black,
                                HorizontalAlignment = global::System.Windows.HorizontalAlignment.Center,
                                VerticalAlignment = global::System.Windows.VerticalAlignment.Center
                            }
                        }
                    };

                    idWin.Show();
                    Task.Delay(3000).ContinueWith(_ => global::System.Windows.Application.Current.Dispatcher.Invoke(idWin.Close));
                });
                return true;
            });
        }
    }
}

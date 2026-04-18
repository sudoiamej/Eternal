using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using Eternal.Models;
using System.Management;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using System.Windows.Media;
using Windows.Media.MediaProperties;
using System.Runtime.InteropServices;

namespace Eternal.ViewModels.Modules
{
    public partial class ComponentsViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty] private ObservableCollection<HardwareComponent> _components;
        [ObservableProperty] private HardwareComponent? _selectedComponent;
        
        // Camera properties
        [ObservableProperty] private WriteableBitmap? _cameraFeed;
        [ObservableProperty] private ObservableCollection<MediaFrameSourceGroup> _cameraGroups = new();
        [ObservableProperty] private MediaFrameSourceGroup? _selectedCameraGroup;
        [ObservableProperty] private string _cameraStatus = "Idle";
        [ObservableProperty] private string _frameInfo = "No frames received";
        private MediaCapture? _mediaCapture;
        private MediaFrameReader? _frameReader;
        private int _frameCount = 0;
        private byte[]? _pixels;
        private int _lastW, _lastH;

        // Keyboard properties
        public ObservableCollection<ObservableCollection<KeyModel>> KeyboardRows { get; } = new ObservableCollection<ObservableCollection<KeyModel>>();
        [ObservableProperty] private int _activeKeysCount;

        // USB properties
        public ObservableCollection<UsbEvent> UsbHistory { get; } = new ObservableCollection<UsbEvent>();
        private ManagementEventWatcher? _usbWatcher;

        // Mouse properties
        [ObservableProperty] private bool _isLeftClicked;
        [ObservableProperty] private bool _isRightClicked;
        [ObservableProperty] private bool _isMiddleClicked;

        public ComponentsViewModel()
        {
            _components = new ObservableCollection<HardwareComponent>
            {
                new HardwareComponent("Camera", "VideoCamera", HardwareComponentType.Camera),
                new HardwareComponent("Mouse", "MousePointer", HardwareComponentType.Mouse),
                new HardwareComponent("Keyboard", "KeyboardOutline", HardwareComponentType.Keyboard),
                new HardwareComponent("Speakers", "VolumeUp", HardwareComponentType.Speakers),
                new HardwareComponent("Monitor", "Television", HardwareComponentType.Monitor),
                new HardwareComponent("Touchpad", "HandPointerOutline", HardwareComponentType.Touchpad),
                new HardwareComponent("Touchscreen", "HandOUp", HardwareComponentType.Touchscreen),
                new HardwareComponent("USB Ports", "Usb", HardwareComponentType.UsbPorts)
            };

            DetectHardwarePresence();
            InitializeKeyboard();
            StartUsbMonitoring();
            
            SelectedComponent = _components.FirstOrDefault(x => x.IsVisible);
        }

        private void DetectHardwarePresence()
        {
            bool hasTouch = false;
            try
            {
                foreach (var device in System.Windows.Input.Tablet.TabletDevices.Cast<System.Windows.Input.TabletDevice>())
                {
                    if (device.Type == System.Windows.Input.TabletDeviceType.Touch)
                    {
                        hasTouch = true;
                        break;
                    }
                }
            }
            catch { }

            bool hasTouchpad = false;
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PointingDevice");
                foreach (var obj in searcher.Get())
                {
                    var description = obj["Description"]?.ToString()?.ToLower() ?? "";
                    if (description.Contains("touchpad") || description.Contains("trackpad") || description.Contains("synaptics") || description.Contains("elan"))
                    {
                        hasTouchpad = true;
                        break;
                    }
                }
            }
            catch { }

            var touchComponent = _components.FirstOrDefault(x => x.Type == HardwareComponentType.Touchscreen);
            if (touchComponent != null) touchComponent.IsVisible = hasTouch;

            var padComponent = _components.FirstOrDefault(x => x.Type == HardwareComponentType.Touchpad);
            if (padComponent != null) padComponent.IsVisible = hasTouchpad;
        }

        partial void OnSelectedComponentChanged(HardwareComponent? value)
        {
            foreach (var c in Components) c.IsSelected = c == value;
            
            if (value?.Type != HardwareComponentType.Camera)
            {
                _ = StopCameraAsync();
            }
            else
            {
                _ = LoadCamerasAndStartAsync();
            }
        }

        private async Task LoadCamerasAndStartAsync()
        {
            try
            {
                CameraStatus = "Searching for devices...";
                var groups = await MediaFrameSourceGroup.FindAllAsync();
                CameraGroups.Clear();
                
                foreach (var group in groups)
                {
                    if (group.SourceInfos.Any(s => s.MediaStreamType == MediaStreamType.VideoPreview || s.MediaStreamType == MediaStreamType.VideoRecord))
                    {
                        CameraGroups.Add(group);
                    }
                }

                if (SelectedCameraGroup == null)
                    SelectedCameraGroup = CameraGroups.FirstOrDefault();
                else
                    await StartCameraAsync();
            }
            catch (Exception ex)
            {
                CameraStatus = "Error: " + ex.Message;
            }
        }

        partial void OnSelectedCameraGroupChanged(MediaFrameSourceGroup? value)
        {
            if (SelectedComponent?.Type == HardwareComponentType.Camera)
            {
                _ = RefreshCameraAsync();
            }
        }

        [RelayCommand]
        private async Task RefreshCameraAsync()
        {
            await StopCameraAsync();
            await StartCameraAsync();
        }

        private void InitializeKeyboard()
        {
            var row1 = new ObservableCollection<KeyModel> {
                new KeyModel("Esc", "ESCAPE", 60), new KeyModel("F1", "F1"), new KeyModel("F2", "F2"), new KeyModel("F3", "F3"), 
                new KeyModel("F4", "F4"), new KeyModel("F5", "F5"), new KeyModel("F6", "F6"), new KeyModel("F7", "F7"), 
                new KeyModel("F8", "F8"), new KeyModel("F9", "F9"), new KeyModel("F10", "F10"), new KeyModel("F11", "F11"), new KeyModel("F12", "F12")
            };
            var row2 = new ObservableCollection<KeyModel> {
                new KeyModel("~", "OemTilde"), new KeyModel("1", "D1"), new KeyModel("2", "D2"), new KeyModel("3", "D3"), 
                new KeyModel("4", "D4"), new KeyModel("5", "D5"), new KeyModel("6", "D6"), new KeyModel("7", "D7"), 
                new KeyModel("8", "D8"), new KeyModel("9", "D9"), new KeyModel("0", "D0"), new KeyModel("-", "OemMinus"), 
                new KeyModel("=", "OemPlus"), new KeyModel("Back", "Back", 90)
            };
            var row3 = new ObservableCollection<KeyModel> {
                new KeyModel("Tab", "Tab", 75), new KeyModel("Q", "Q"), new KeyModel("W", "W"), new KeyModel("E", "E"), 
                new KeyModel("R", "R"), new KeyModel("T", "T"), new KeyModel("Y", "Y"), new KeyModel("U", "U"), 
                new KeyModel("I", "I"), new KeyModel("O", "O"), new KeyModel("P", "P"), new KeyModel("[", "OemOpenBrackets"), 
                new KeyModel("]", "OemCloseBrackets"), new KeyModel("\\", "OemBackslash", 75)
            };
            var row4 = new ObservableCollection<KeyModel> {
                new KeyModel("Caps", "Capital", 90), new KeyModel("A", "A"), new KeyModel("S", "S"), new KeyModel("D", "D"), 
                new KeyModel("F", "F"), new KeyModel("G", "G"), new KeyModel("H", "H"), new KeyModel("J", "J"), 
                new KeyModel("K", "K"), new KeyModel("L", "L"), new KeyModel(";", "OemSemicolon"), new KeyModel("'", "OemQuotes"), 
                new KeyModel("Enter", "Return", 115)
            };
            var row5 = new ObservableCollection<KeyModel> {
                new KeyModel("Shift", "LeftShift", 120), new KeyModel("Z", "Z"), new KeyModel("X", "X"), new KeyModel("C", "C"), 
                new KeyModel("V", "V"), new KeyModel("B", "B"), new KeyModel("N", "N"), new KeyModel("M", "M"), 
                new KeyModel(",", "OemComma"), new KeyModel(".", "OemPeriod"), new KeyModel("/", "OemQuestion"), new KeyModel("Shift", "RightShift", 120)
            };
            var row6 = new ObservableCollection<KeyModel> {
                new KeyModel("Ctrl", "LeftCtrl", 70), new KeyModel("Win", "LWin", 70), new KeyModel("Alt", "LeftAlt", 70), 
                new KeyModel("Space", "Space", 350), new KeyModel("Alt", "RightAlt", 70), new KeyModel("Win", "RWin", 70), 
                new KeyModel("App", "Apps", 70), new KeyModel("Ctrl", "RightCtrl", 70)
            };

            KeyboardRows.Add(row1);
            KeyboardRows.Add(row2);
            KeyboardRows.Add(row3);
            KeyboardRows.Add(row4);
            KeyboardRows.Add(row5);
            KeyboardRows.Add(row6);
        }

        public void HandleKeyDown(string key)
        {
            var k = KeyboardRows.SelectMany(r => r).FirstOrDefault(x => x.KeyCode.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (k != null) k.IsPressed = true;
            UpdateActiveKeysCount();
        }

        public void HandleKeyUp(string key)
        {
            var k = KeyboardRows.SelectMany(r => r).FirstOrDefault(x => x.KeyCode.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (k != null) k.IsPressed = false;
            UpdateActiveKeysCount();
        }

        private void UpdateActiveKeysCount()
        {
            ActiveKeysCount = KeyboardRows.SelectMany(r => r).Count(x => x.IsPressed);
        }

        private async Task StartCameraAsync()
        {
            try
            {
                if (_mediaCapture != null || SelectedCameraGroup == null) return;

                CameraStatus = "Initializing Camera...";
                _frameCount = 0;
                _mediaCapture = new MediaCapture();
                
                var settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup = SelectedCameraGroup,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };

                await _mediaCapture.InitializeAsync(settings);

                var frameSource = _mediaCapture.FrameSources.Values
                    .FirstOrDefault(s => s.Info.MediaStreamType == MediaStreamType.VideoPreview) 
                    ?? _mediaCapture.FrameSources.Values
                    .FirstOrDefault(s => s.Info.MediaStreamType == MediaStreamType.VideoRecord);

                if (frameSource != null)
                {
                    _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Bgra8);
                    if (_frameReader == null) _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);

                    if (_frameReader != null)
                    {
                        _frameReader.FrameArrived += FrameReader_FrameArrived;
                        var startStatus = await _frameReader.StartAsync();
                        CameraStatus = startStatus == MediaFrameReaderStartStatus.Success ? "Streaming Live" : "Capture Failed";
                    }
                }
            }
            catch (Exception ex)
            {
                CameraStatus = "Error: " + ex.Message;
            }
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                var videoFrame = frame?.VideoMediaFrame;
                var softwareBitmap = videoFrame?.SoftwareBitmap;
                
                if (softwareBitmap == null && videoFrame?.Direct3DSurface != null)
                {
                    // If camera provides Direct3D surface, try to get SoftwareBitmap from it
                    Task.Run(async () => {
                        try { softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface); } catch {}
                    }).Wait();
                }

                if (softwareBitmap != null)
                {
                    _frameCount++;
                    
                    // Convert to standard format for display and copy DIMENSIONS before disposing
                    using (var displayBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore))
                    {
                        int w = displayBitmap.PixelWidth;
                        int h = displayBitmap.PixelHeight;
                        int size = w * h * 4;

                        // Create pixel buffer and copy data OUT of the displayBitmap immediately
                        // This prevents the "0x0" resolution issue caused by disposal race conditions
                        if (_pixels == null || _pixels.Length != size) _pixels = new byte[size];
                        displayBitmap.CopyToBuffer(_pixels.AsBuffer());

                        _lastW = w; _lastH = h;

                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try 
                            {
                                FrameInfo = $"Frames: {_frameCount} | Resolution: {_lastW}x{_lastH}";

                                if (CameraFeed == null || CameraFeed.PixelWidth != _lastW || CameraFeed.PixelHeight != _lastH)
                                {
                                    CameraFeed = new WriteableBitmap(_lastW, _lastH, 96, 96, PixelFormats.Bgr32, null);
                                    OnPropertyChanged(nameof(CameraFeed));
                                }

                                CameraFeed.Lock();
                                Marshal.Copy(_pixels, 0, CameraFeed.BackBuffer, _pixels.Length);
                                CameraFeed.AddDirtyRect(new Int32Rect(0, 0, _lastW, _lastH));
                                CameraFeed.Unlock();
                            }
                            catch { }
                        }));
                    }
                }
            }
        }

        private async Task StopCameraAsync()
        {
            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= FrameReader_FrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }

            CameraFeed = null;
            CameraStatus = "Idle";
            FrameInfo = "No frames received";
        }

        [RelayCommand]
        private void PlayTestSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        [RelayCommand]
        private void StartMonitorTest()
        {
            var monitorWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Topmost = true,
                Background = System.Windows.Media.Brushes.Red,
                Cursor = System.Windows.Input.Cursors.None
            };

            int colorIndex = 0;
            System.Windows.Media.Brush[] colors = { System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Green, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.White, System.Windows.Media.Brushes.Black };

            monitorWindow.MouseDown += (s, e) =>
            {
                colorIndex++;
                if (colorIndex >= colors.Length) monitorWindow.Close();
                else monitorWindow.Background = colors[colorIndex];
            };

            monitorWindow.KeyDown += (s, e) => monitorWindow.Close();

            monitorWindow.ShowDialog();
        }

        private void StartUsbMonitoring()
        {
            try
            {
                _usbWatcher = new ManagementEventWatcher();
                var query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity'");
                _usbWatcher.Query = query;
                _usbWatcher.EventArrived += (s, e) =>
                {
                    var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                    var pnpId = instance["PNPDeviceID"]?.ToString() ?? "";
                    
                    if (pnpId.Contains("USB"))
                    {
                        var name = instance["Name"]?.ToString() ?? "Unknown USB Device";
                        var eventType = e.NewEvent.ClassPath.ClassName;
                        var action = eventType.Contains("Creation") ? "Connected" : (eventType.Contains("Deletion") ? "Disconnected" : null);

                        if (action != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                UsbHistory.Insert(0, new UsbEvent { Timestamp = DateTime.Now, DeviceName = name, Action = action });
                            });
                        }
                    }
                };
                _usbWatcher.Start();
            }
            catch { }
        }

        public void Suspend()
        {
            _ = StopCameraAsync();
            _usbWatcher?.Stop();
        }

        public void Resume()
        {
            if (SelectedComponent?.Type == HardwareComponentType.Camera)
            {
                _ = LoadCamerasAndStartAsync();
            }
            try { _usbWatcher?.Start(); } catch { }
        }

        public void Dispose()
        {
            _ = StopCameraAsync();
            _usbWatcher?.Stop();
            _usbWatcher?.Dispose();
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DB0-465B-BD45-4D80ED361535")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}

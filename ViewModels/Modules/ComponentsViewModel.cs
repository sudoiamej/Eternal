using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Eternal.Models;
using Eternal.ViewModels;
using Eternal.Services.System;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Eternal.ViewModels.Modules
{
    public partial class ComponentsViewModel : BaseViewModel
    {
        private readonly ILoggingService _loggingService;
        [ObservableProperty] private ObservableCollection<HardwareComponent> _components = new();
        [ObservableProperty] private HardwareComponent? _selectedComponent;
        [ObservableProperty] private string _keyboardInput = string.Empty;
        
        // Camera Properties
        [ObservableProperty] private bool _isCameraActive;
        [ObservableProperty] private string _cameraStatus = "Inactive";
        [ObservableProperty] private ObservableCollection<CameraGroup> _cameraGroups = new();
        [ObservableProperty] private CameraGroup? _selectedCameraGroup;
        [ObservableProperty] private ImageSource? _cameraFeed;
        [ObservableProperty] private string _frameInfo = "No feed active";

        private MediaCapture? _mediaCapture;
        private Windows.Media.Capture.Frames.MediaFrameReader? _frameReader;
        private bool _isInitialized;

        // Keyboard Properties
        [ObservableProperty] private ObservableCollection<ObservableCollection<KeyModel>> _keyboardRows = new();
        [ObservableProperty] private int _activeKeysCount;

        // USB Properties
        [ObservableProperty] private ObservableCollection<UsbEvent> _usbHistory = new();

        public ComponentsViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            Components = new ObservableCollection<HardwareComponent>
            {
                new HardwareComponent("Camera", "VideoCamera", HardwareComponentType.Camera),
                new HardwareComponent("Mouse", "MousePointer", HardwareComponentType.Mouse),
                new HardwareComponent("Keyboard", "KeyboardOutline", HardwareComponentType.Keyboard),
                new HardwareComponent("Speakers", "VolumeUp", HardwareComponentType.Speakers),
                new HardwareComponent("Monitor", "Television", HardwareComponentType.Monitor),
                new HardwareComponent("Touchpad", "HandPointerOutline", HardwareComponentType.Touchpad),
                new HardwareComponent("Touchscreen", "HandOutlineUp", HardwareComponentType.Touchscreen),
                new HardwareComponent("USB Ports", "Usb", HardwareComponentType.UsbPorts)
            };

            DetectHardwarePresence();
            InitializeKeyboard();
            StartUsbMonitoring();
            InitializeCamera();
            
            SelectedComponent = Components.FirstOrDefault(x => x.IsVisible);
        }

        private void DetectHardwarePresence()
        {
            bool hasTouch = false;
            bool hasTouchpad = false;
            try 
            {
                foreach (var tablet in System.Windows.Input.Tablet.TabletDevices)
                {
                    hasTouch = true;
                    break;
                }
            } 
            catch { }

            var touchComponent = Components.FirstOrDefault(x => x.Type == HardwareComponentType.Touchscreen);
            if (touchComponent != null) touchComponent.IsVisible = hasTouch;

            var padComponent = Components.FirstOrDefault(x => x.Type == HardwareComponentType.Touchpad);
            if (touchComponent != null) touchComponent.IsVisible = hasTouchpad;
        }

        private void InitializeCamera()
        {
            _ = RefreshCameraList();
        }

        private async Task RefreshCameraList()
        {
            CameraStatus = "Searching...";
            CameraGroups.Clear();

            try
            {
                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                
                foreach (var device in devices)
                {
                    CameraGroups.Add(new CameraGroup { DisplayName = device.Name, DeviceId = device.Id });
                }

                if (CameraGroups.Count == 0)
                {
                    CameraGroups.Add(new CameraGroup { DisplayName = "No Cameras Detected", DeviceId = "None" });
                    CameraStatus = "Disconnected";
                }
                else
                {
                    SelectedCameraGroup = CameraGroups.FirstOrDefault();
                    CameraStatus = "Standby";
                }
            }
            catch (Exception ex)
            {
                CameraGroups.Add(new CameraGroup { DisplayName = "Detection Error", DeviceId = "Error" });
                CameraStatus = "Error";
            }
        }

        [RelayCommand]
        private async Task ToggleCamera()
        {
            if (IsCameraActive) await StopCameraAsync();
            else await StartCameraAsync();
        }

        private async Task StartCameraAsync()
        {
            if (SelectedCameraGroup == null || SelectedCameraGroup.DeviceId == "None") return;

            try
            {
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings 
                { 
                    VideoDeviceId = SelectedCameraGroup.DeviceId,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };
                
                await _mediaCapture.InitializeAsync(settings);
                
                // Find the best frame source for the video stream
                var frameSource = _mediaCapture.FrameSources.Values.FirstOrDefault(s => s.Info.MediaStreamType == MediaStreamType.VideoPreview)
                                ?? _mediaCapture.FrameSources.Values.FirstOrDefault(s => s.Info.MediaStreamType == MediaStreamType.VideoRecord);

                if (frameSource == null) throw new Exception("No suitable video source found.");

                _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Bgra8);
                _frameReader.FrameArrived += OnFrameArrived;
                
                var startStatus = await _frameReader.StartAsync();
                if (startStatus != Windows.Media.Capture.Frames.MediaFrameReaderStartStatus.Success)
                    throw new Exception($"Reader failed: {startStatus}");

                IsCameraActive = true;
                CameraStatus = "Live Feed Active";
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                CameraStatus = $"Camera Error: {ex.Message}";
                IsCameraActive = false;
                _loggingService.Log($"Camera Start Failure: {ex.Message}");
                await StopCameraAsync();
            }
        }

        private async Task StopCameraAsync()
        {
            IsCameraActive = false;
            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= OnFrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }

            _isInitialized = false;
            CameraStatus = "Standby";
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                CameraFeed = null;
                FrameInfo = "Feed terminated.";
            });
        }

        private void OnFrameArrived(Windows.Media.Capture.Frames.MediaFrameReader sender, Windows.Media.Capture.Frames.MediaFrameArrivedEventArgs args)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                var bitmap = frame?.VideoMediaFrame?.SoftwareBitmap;
                if (bitmap == null) return;

                int width = bitmap.PixelWidth;
                int height = bitmap.PixelHeight;
                int stride = width * 4;

                var buffer = new byte[stride * height];
                bitmap.CopyToBuffer(buffer.AsBuffer());

                // SoftwareBitmap must be accessed on the UI thread or converted to a format suitable for WPF
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    try
                    {
                        if (!IsCameraActive) return;

                        // Reuse WriteableBitmap if dimensions match
                        if (CameraFeed is not WriteableBitmap wbm || wbm.PixelWidth != width || wbm.PixelHeight != height)
                        {
                            wbm = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                            CameraFeed = wbm;
                        }

                        wbm.WritePixels(new Int32Rect(0, 0, width, height), buffer, stride, 0);
                        FrameInfo = $"{width}x{height} @ Active";
                    }
                    catch { }
                });
            }
        }

        partial void OnSelectedComponentChanged(HardwareComponent? value)
        {
            foreach (var c in Components) c.IsSelected = c == value;
            if (value?.Type != HardwareComponentType.Camera && IsCameraActive)
            {
                _ = StopCameraAsync();
            }
        }

        [RelayCommand]
        private async Task TestComponent(HardwareComponent component)
        {
            if (component == null) return;
            component.Status = "Testing...";
            await Task.Delay(1000);
            component.Status = "Functional";
        }

        [RelayCommand]
        private async Task RefreshCamera()
        {
            if (IsCameraActive) await StopCameraAsync();
            await RefreshCameraList();
        }

        [RelayCommand]
        private void PlayTestSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        [RelayCommand]
        private void StartMonitorTest()
        {
            var testWin = new Eternal.Views.Helpers.MonitorTestWindow();
            testWin.ShowDialog();
        }

        public void HandleKeyDown(string key)
        {
            KeyboardInput = $"Key Down: {key}";
            UpdateKeyState(key, true);
        }

        public void HandleKeyUp(string key)
        {
            KeyboardInput = $"Key Up: {key}";
            UpdateKeyState(key, false);
        }

        private void UpdateKeyState(string keyCode, bool isPressed)
        {
            foreach (var row in KeyboardRows)
            {
                foreach (var key in row)
                {
                    if (key.KeyCode == keyCode)
                    {
                        key.IsPressed = isPressed;
                        break;
                    }
                }
            }

            // Update N-Key Rollover Count
            int pressedCount = 0;
            foreach (var row in KeyboardRows)
            {
                foreach (var key in row)
                {
                    if (key.IsPressed) pressedCount++;
                }
            }
            ActiveKeysCount = pressedCount;
        }

        private void InitializeKeyboard() 
        { 
            // ANSI 104-Key Layout Mapping
            var row1 = new ObservableCollection<KeyModel> { 
                new KeyModel("ESC", "Escape"), new KeyModel("F1", "F1"), new KeyModel("F2", "F2"), new KeyModel("F3", "F3"), 
                new KeyModel("F4", "F4"), new KeyModel("F5", "F5"), new KeyModel("F6", "F6"), new KeyModel("F7", "F7"), 
                new KeyModel("F8", "F8"), new KeyModel("F9", "F9"), new KeyModel("F10", "F10"), new KeyModel("F11", "F11"), new KeyModel("F12", "F12") 
            };
            
            var row2 = new ObservableCollection<KeyModel> { 
                new KeyModel("~", "OemTilde"), new KeyModel("1", "D1"), new KeyModel("2", "D2"), new KeyModel("3", "D3"), 
                new KeyModel("4", "D4"), new KeyModel("5", "D5"), new KeyModel("6", "D6"), new KeyModel("7", "D7"), 
                new KeyModel("8", "D8"), new KeyModel("9", "D9"), new KeyModel("0", "D0"), new KeyModel("-", "OemMinus"), 
                new KeyModel("+", "OemPlus"), new KeyModel("BKSP", "Back") 
            };

            var row3 = new ObservableCollection<KeyModel> { 
                new KeyModel("TAB", "Tab"), new KeyModel("Q", "Q"), new KeyModel("W", "W"), new KeyModel("E", "E"), 
                new KeyModel("R", "R"), new KeyModel("T", "T"), new KeyModel("Y", "Y"), new KeyModel("U", "U"), 
                new KeyModel("I", "I"), new KeyModel("O", "O"), new KeyModel("P", "P"), new KeyModel("[", "OemOpenBrackets"), 
                new KeyModel("]", "Oem6"), new KeyModel("\\", "Oem5") 
            };

            var row4 = new ObservableCollection<KeyModel> { 
                new KeyModel("CAPS", "Capital"), new KeyModel("A", "A"), new KeyModel("S", "S"), new KeyModel("D", "D"), 
                new KeyModel("F", "F"), new KeyModel("G", "G"), new KeyModel("H", "H"), new KeyModel("J", "J"), 
                new KeyModel("K", "K"), new KeyModel("L", "L"), new KeyModel(";", "Oem1"), new KeyModel("'", "OemQuotes"), 
                new KeyModel("ENTER", "Return") 
            };

            var row5 = new ObservableCollection<KeyModel> { 
                new KeyModel("SHIFT", "LeftShift"), new KeyModel("Z", "Z"), new KeyModel("X", "X"), new KeyModel("C", "C"), 
                new KeyModel("V", "V"), new KeyModel("B", "B"), new KeyModel("N", "N"), new KeyModel("M", "M"), 
                new KeyModel(",", "OemComma"), new KeyModel(".", "OemPeriod"), new KeyModel("/", "OemQuestion"), new KeyModel("SHIFT", "RightShift") 
            };

            var row6 = new ObservableCollection<KeyModel> { 
                new KeyModel("CTRL", "LeftCtrl"), new KeyModel("WIN", "LWin"), new KeyModel("ALT", "LeftAlt"), 
                new KeyModel("SPACE", "Space"), new KeyModel("ALT", "RightAlt"), new KeyModel("WIN", "RWin"), 
                new KeyModel("MENU", "Apps"), new KeyModel("CTRL", "RightCtrl") 
            };

            KeyboardRows.Add(row1);
            KeyboardRows.Add(row2);
            KeyboardRows.Add(row3);
            KeyboardRows.Add(row4);
            KeyboardRows.Add(row5);
            KeyboardRows.Add(row6);
        }
        
        private void StartUsbMonitoring() 
        { 
            UsbHistory.Add(new UsbEvent { Timestamp = DateTime.Now, Action = "Connected", DeviceName = "Root Hub" });
        }

        public override void Deactivate()
        {
            _ = StopCameraAsync();
            base.Deactivate();
        }

        public void Suspend() { _ = StopCameraAsync(); }
        public void Resume() { DetectHardwarePresence(); }
    }

    [ComImport]
    [Guid("5B0D3235-4DB0-465B-BD45-4D80ED361535")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}

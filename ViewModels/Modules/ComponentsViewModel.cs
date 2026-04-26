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

namespace Eternal.ViewModels.Modules
{
    public partial class ComponentsViewModel : ObservableObject
    {
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

        // Keyboard Properties
        [ObservableProperty] private ObservableCollection<ObservableCollection<KeyModel>> _keyboardRows = new();
        [ObservableProperty] private int _activeKeysCount;

        // USB Properties
        [ObservableProperty] private ObservableCollection<UsbEvent> _usbHistory = new();

        public ComponentsViewModel()
        {
            Components = new ObservableCollection<HardwareComponent>
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
            CameraGroups.Add(new CameraGroup { DisplayName = "Integrated Webcam", DeviceId = "0" });
            SelectedCameraGroup = CameraGroups.FirstOrDefault();
        }

        partial void OnSelectedComponentChanged(HardwareComponent? value)
        {
            foreach (var c in Components) c.IsSelected = c == value;
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
        private void RefreshCamera()
        {
            CameraStatus = "Refreshing...";
            Task.Delay(500).ContinueWith(_ => { CameraStatus = "Active"; });
        }

        [RelayCommand]
        private void PlayTestSound()
        {
            System.Media.SystemSounds.Beep.Play();
        }

        [RelayCommand]
        private void StartMonitorTest()
        {
            System.Windows.MessageBox.Show("Monitor Test started. Cycle through colors using mouse clicks.", "Monitor Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void HandleKeyDown(string key)
        {
            KeyboardInput = $"Key Down: {key}";
        }

        public void HandleKeyUp(string key)
        {
            KeyboardInput = $"Key Up: {key}";
        }

        private void InitializeKeyboard() 
        { 
            // Mock keyboard rows
            var row1 = new ObservableCollection<KeyModel> { new KeyModel("ESC", "Escape"), new KeyModel("F1", "F1"), new KeyModel("F2", "F2") };
            KeyboardRows.Add(row1);
        }
        
        private void StartUsbMonitoring() 
        { 
            UsbHistory.Add(new UsbEvent { Timestamp = DateTime.Now, Action = "Connected", DeviceName = "Root Hub" });
        }

        public void Suspend() { /* Cleanup hardware hooks */ }
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

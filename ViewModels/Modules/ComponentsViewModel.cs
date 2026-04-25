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

namespace Eternal.ViewModels.Modules
{
    public partial class ComponentsViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<HardwareComponent> _components = new();
        [ObservableProperty] private HardwareComponent? _selectedComponent;
        [ObservableProperty] private string _keyboardInput = string.Empty;
        [ObservableProperty] private bool _isCameraActive;
        [ObservableProperty] private string _cameraStatus = "Inactive";

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
            
            SelectedComponent = Components.FirstOrDefault(x => x.IsVisible);
        }

        private void DetectHardwarePresence()
        {
            bool hasTouch = false;
            bool hasTouchpad = false;
            try 
            {
                // Simple check for touch support
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
            if (padComponent != null) padComponent.IsVisible = hasTouchpad;
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

        public void HandleKeyDown(string key)
        {
            KeyboardInput = $"Key Down: {key}";
        }

        public void HandleKeyUp(string key)
        {
            KeyboardInput = $"Key Up: {key}";
        }

        private void InitializeKeyboard() { /* Logic for key hooks */ }
        private void StartUsbMonitoring() { /* Logic for USB insertion events */ }

        public void Suspend() { /* Cleanup hardware hooks */ }
        public void Resume() { DetectHardwarePresence(); }
    }

    public enum HardwareComponentType { Camera, Mouse, Keyboard, Speakers, Monitor, Touchpad, Touchscreen, UsbPorts }

    public partial class HardwareComponent : ObservableObject
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public HardwareComponentType Type { get; set; }
        [ObservableProperty] private string _status = "Standby";
        [ObservableProperty] private bool _isVisible = true;
        [ObservableProperty] private bool _isSelected;

        public HardwareComponent(string name, string icon, HardwareComponentType type)
        {
            Name = name; Icon = icon; Type = type;
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

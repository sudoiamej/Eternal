using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Eternal.Models
{
    public enum HardwareComponentType
    {
        Camera,
        Mouse,
        Keyboard,
        Speakers,
        Monitor,
        Touchpad,
        Touchscreen,
        UsbPorts
    }

    public partial class HardwareComponent : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public HardwareComponentType Type { get; set; }
        [ObservableProperty] private string _status = "Standby";
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isVisible = true;

        public HardwareComponent(string name, string icon, HardwareComponentType type)
        {
            Name = name;
            Icon = icon;
            Type = type;
        }
    }

    public partial class KeyModel : ObservableObject
    {
        public string Display { get; set; } = string.Empty;
        public string KeyCode { get; set; } = string.Empty;
        public double Width { get; set; } = 50;
        [ObservableProperty] private bool _isPressed;

        public KeyModel(string display, string code, double width = 50)
        {
            Display = display;
            KeyCode = code;
            Width = width;
        }
    }

    public class CameraGroup
    {
        public string DisplayName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
    }

    public class UsbEvent
    {
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "Connected" or "Disconnected"
    }
}

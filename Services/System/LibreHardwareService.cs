using System;
using LibreHardwareMonitor.Hardware;

namespace Eternal.Services.System
{
    public interface ILibreHardwareService
    {
        Computer Computer { get; }
        void Update();
    }

    public class WindowsLibreHardwareService : ILibreHardwareService, IDisposable
    {
        private readonly Computer _computer;
        private readonly UpdateVisitor _visitor;
        private bool _isOpened = false;

        public Computer Computer => _computer;

        public WindowsLibreHardwareService()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true,
                IsBatteryEnabled = true
            };
            _visitor = new UpdateVisitor();
            try
            {
                _computer.Open();
                _isOpened = true;
            }
            catch { }
        }

        public void Update()
        {
            if (!_isOpened) return;
            try
            {
                _computer.Accept(_visitor);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_isOpened)
            {
                try { _computer.Close(); } catch { }
            }
        }
    }
}

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
        private Computer? _computer;
        private UpdateVisitor? _visitor;
        private bool _isOpened = false;
        private readonly object _lock = new object();

        public Computer Computer
        {
            get
            {
                EnsureInitialized();
                return _computer!;
            }
        }

        private void EnsureInitialized()
        {
            if (_isOpened) return;
            lock (_lock)
            {
                if (_isOpened) return;
                try
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
                    _computer.Open();
                    _isOpened = true;
                }
                catch { }
            }
        }

        public void Update()
        {
            if (!_isOpened || _computer == null || _visitor == null) return;
            try
            {
                _computer.Accept(_visitor);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_isOpened && _computer != null)
            {
                try 
                { 
                    _computer.Close(); 
                    _isOpened = false;
                } 
                catch { }
            }
        }
    }
}

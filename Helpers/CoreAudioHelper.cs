using System;
using System.Runtime.InteropServices;

namespace Eternal.Helpers
{
    public static class CoreAudioHelper
    {
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject { }

        [Guid("A9566477-36A4-4F78-ABE0-B5B5D5B6001E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            void EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
            void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            void Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            void RegisterControlChangeNotify(IntPtr pNotify);
            void UnregisterControlChangeNotify(IntPtr pNotify);
            void GetChannelCount(out uint pnChannelCount);
            void SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            void SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            void GetMasterVolumeLevel(out float pfLevelDB);
            void GetMasterVolumeLevelScalar(out float pfLevel);
        }

        public static float GetMasterVolume()
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice dev); // eRender = 0, eConsole = 0
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                dev.Activate(ref iid, 23, IntPtr.Zero, out object epvObj); // CLSCTX_ALL = 23
                var epv = (IAudioEndpointVolume)epvObj;
                epv.GetMasterVolumeLevelScalar(out float level);
                return level * 100f;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMasterVolume WASAPI Error: {ex.Message}");
                return 80f;
            }
        }

        public static void SetMasterVolume(float levelPercent)
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice dev); // eRender = 0, eConsole = 0
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                dev.Activate(ref iid, 23, IntPtr.Zero, out object epvObj);
                var epv = (IAudioEndpointVolume)epvObj;
                Guid ctx = Guid.Empty;
                float scalar = Math.Clamp(levelPercent / 100f, 0f, 1f);
                epv.SetMasterVolumeLevelScalar(scalar, ref ctx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetMasterVolume WASAPI Error: {ex.Message}");
            }
        }
    }
}

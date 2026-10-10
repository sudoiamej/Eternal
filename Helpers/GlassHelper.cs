using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Eternal.Helpers
{
    public static class GlassHelper
    {
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // DWM SystemBackdrop Types (Windows 11 Build 22000+)
        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMSBT_TRANSLUCENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt

        public static bool EnableAcrylicGlass(Window window)
        {
            if (window == null) return false;

            try
            {
                var helper = new WindowInteropHelper(window);
                IntPtr hwnd = helper.Handle;

                if (hwnd == IntPtr.Zero) return false;

                // Force Immersive Dark Mode for DWM Frame
                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                // Apply Translucent Acrylic Glass Backdrop (DWMSBT_TRANSLUCENTWINDOW = 3)
                int backdropType = DWMSBT_TRANSLUCENTWINDOW;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

                return hr == 0;
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"GlassHelper Error: {ex.Message}");
                return false;
            }
        }

        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}

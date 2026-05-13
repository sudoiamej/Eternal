using System;

namespace Eternal.Helpers
{
    public static class OsHelper
    {
        /// <summary>
        /// Detects if the current system is Windows 11 or higher (Build 22000+).
        /// </summary>
        public static bool IsWindows11OrGreater()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && os.Version.Build >= 22000;
        }

        /// <summary>
        /// Detects if the current system is Windows 10 or higher.
        /// </summary>
        public static bool IsWindows10OrGreater()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && os.Version.Major >= 10;
        }
    }
}

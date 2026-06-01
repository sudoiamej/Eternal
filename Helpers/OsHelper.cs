using System;

namespace Eternal.Helpers
{
    public static class OsHelper
    {
        public const int Build_Win10_1507 = 10240;
        public const int Build_Win10_1903 = 18362;
        public const int Build_Win10_2004 = 19041;
        public const int Build_Win11_21H2 = 22000;
        public const int Build_Win11_22H2 = 22621;

        /// <summary>
        /// Gets the current Windows build number.
        /// </summary>
        public static int GetCurrentBuild()
        {
            return Environment.OSVersion.Version.Build;
        }

        /// <summary>
        /// Validates if the current OS build falls within the specified range.
        /// </summary>
        public static bool IsBuildSupported(int? minBuild, int? maxBuild)
        {
            int currentBuild = GetCurrentBuild();
            if (minBuild.HasValue && currentBuild < minBuild.Value) return false;
            if (maxBuild.HasValue && currentBuild > maxBuild.Value) return false;
            return true;
        }

        /// <summary>
        /// Detects if the current system is Windows 11 or higher (Build 22000+).
        /// </summary>
        public static bool IsWindows11OrGreater()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT && GetCurrentBuild() >= Build_Win11_21H2;
        }

        /// <summary>
        /// Detects if the current system is Windows 10 or higher.
        /// </summary>
        public static bool IsWindows10OrGreater()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10;
        }
    }
}

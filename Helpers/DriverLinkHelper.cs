using System;
using Eternal.Services.System;

namespace Eternal.Helpers
{
    public static class DriverLinkHelper
    {
        public static string GenerateOfficialSupportLink(DriverInfo driver, OemSupportInfo oem)
        {
            string hwid = driver.HardwareId.ToUpper();
            string provider = driver.Provider.ToUpper();

            // 1. Specific Vendor Checks (Hardware Level)
            if (hwid.Contains("VEN_10DE")) return "https://www.nvidia.com/Download/index.aspx";
            if (hwid.Contains("VEN_1002")) return "https://www.amd.com/en/support";
            if (hwid.Contains("VEN_8086")) return "https://www.intel.com/content/www/us/en/support/detect.html";
            if (hwid.Contains("VEN_10EC")) return "https://www.realtek.com/en/downloads";

            // 2. OEM Level Checks (System Level)
            string vendor = oem.Vendor.ToUpper();
            if (vendor.Contains("DELL")) 
                return $"https://www.dell.com/support/home/en-us/product-support/servicetag/{oem.SerialNumber}/drivers";
            
            if (vendor.Contains("HP") || vendor.Contains("HEWLETT-PACKARD"))
                return "https://support.hp.com/us-en/drivers";

            if (vendor.Contains("LENOVO"))
                return "https://pcsupport.lenovo.com/us/en/products/search";

            if (vendor.Contains("ASUS"))
                return "https://www.asus.com/support/Download-Center/";

            if (vendor.Contains("ACER"))
                return "https://www.acer.com/ac/en/US/content/drivers";

            if (vendor.Contains("MSI"))
                return "https://www.msi.com/support/download";

            // 3. Fallback to Google Search for Official Driver
            string query = Uri.EscapeDataString($"{driver.Name} {driver.HardwareId} official driver");
            return $"https://www.google.com/search?q={query}";
        }
    }
}

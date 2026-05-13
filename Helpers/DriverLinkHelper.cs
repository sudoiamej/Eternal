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
            string vendor = oem.Vendor.ToUpper();
            
            // Clean model name (remove 'To be filled by O.E.M.' etc handled by service layer now)
            // But let's get the model from the system if available
            string model = "";
            try {
                using (var searcher = new System.Management.ManagementObjectSearcher("select Product from Win32_BaseBoard"))
                {
                    using var collection = searcher.Get();
                    foreach (var obj in collection)
                    {
                        using (obj)
                        {
                            model = obj["Product"]?.ToString() ?? "";
                            break;
                        }
                    }
                }
            } catch { }

            // 1. Specific Vendor HWID Checks (GPU/Chipset Level)
            if (hwid.Contains("VEN_10DE")) return "https://www.nvidia.com/Download/index.aspx";
            if (hwid.Contains("VEN_1002")) return "https://www.amd.com/en/support";
            if (hwid.Contains("VEN_8086")) return "https://www.intel.com/content/www/us/en/support/detect.html";
            if (hwid.Contains("VEN_10EC")) return "https://www.realtek.com/en/downloads";

            // 2. Direct OEM Support Logic (System/Motherboard Level)
            if (vendor.Contains("DELL")) 
                return $"https://www.dell.com/support/home/en-us/product-support/servicetag/{oem.SerialNumber}/drivers";
            
            if (vendor.Contains("ASUS"))
            {
                if (!string.IsNullOrEmpty(model))
                    return $"https://www.google.com/search?q={Uri.EscapeDataString("ASUS " + model + " Support Drivers")}&btnI"; // btnI is "I'm Feeling Lucky"
                return "https://www.asus.com/support/Download-Center/";
            }

            if (vendor.Contains("MSI"))
            {
                if (!string.IsNullOrEmpty(model))
                    return $"https://www.msi.com/search/{Uri.EscapeDataString(model)}";
                return "https://www.msi.com/support/download";
            }

            if (vendor.Contains("GIGABYTE"))
            {
                if (!string.IsNullOrEmpty(model))
                    return $"https://www.gigabyte.com/Search?kw={Uri.EscapeDataString(model)}#Products";
                return "https://www.gigabyte.com/Support/Motherboard";
            }

            if (vendor.Contains("HP") || vendor.Contains("HEWLETT-PACKARD"))
                return "https://support.hp.com/us-en/drivers";

            if (vendor.Contains("LENOVO"))
                return "https://pcsupport.lenovo.com/us/en/products/search";

            // 3. Smart Fallback for Custom/Generic
            string target = !string.IsNullOrEmpty(model) ? $"{oem.Vendor} {model}" : driver.Name;
            string query = Uri.EscapeDataString($"{target} official drivers download");
            return $"https://www.google.com/search?q={query}";
        }
    }
}

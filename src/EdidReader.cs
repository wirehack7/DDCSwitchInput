using Microsoft.Win32;
using System;
using System.Text;

namespace DdcTraySwitcher
{
    public static class EdidReader
    {
        public static string GetMonitorName(int index)
        {
            int currentIndex = 0;

            try
            {
                using var displayKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Enum\\DISPLAY");
                if (displayKey == null) return $"Monitor {index + 1}";

                foreach (var vendor in displayKey.GetSubKeyNames())
                {
                    using var vendorKey = displayKey.OpenSubKey(vendor);
                    if (vendorKey == null) continue;

                    foreach (var device in vendorKey.GetSubKeyNames())
                    {
                        using var deviceKey = vendorKey.OpenSubKey(device + "\\Device Parameters");
                        var edid = deviceKey?.GetValue("EDID") as byte[];
                        if (edid != null && edid.Length >= 128)
                        {
                            if (currentIndex == index)
                            {
                                for (int offset = 0x36; offset <= 0x6C; offset += 18)
                                {
                                    if (edid[offset] == 0x00 && edid[offset + 1] == 0x00 && edid[offset + 3] == 0xFC)
                                    {
                                        var raw = Encoding.ASCII.GetString(edid, offset + 5, 13);
                                        var cleaned = raw.Replace("\0", "").Replace("\n", "").Replace("\r", "").Trim();
                                        if (!string.IsNullOrWhiteSpace(cleaned))
                                            return cleaned;
                                    }
                                }

                                return $"Monitor {index + 1}";
                            }

                            currentIndex++;
                        }
                    }
                }
            }
            catch
            {
                // do nothing
            }

            return $"Monitor {index + 1}";
        }
    }
}

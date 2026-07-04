using System.Management;
using System.Text.RegularExpressions;

namespace MetaLinkCompatTool;

public static class DeviceInfoService
{
    public static HardwareInfo GetHardwareInfo()
    {
        var cpuName = "Unknown CPU";
        using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
        {
            foreach (ManagementObject item in searcher.Get())
            {
                cpuName = Convert.ToString(item["Name"])?.Trim() ?? cpuName;
                break;
            }
        }

        var gpus = new List<GpuInfo>();
        using (var searcher = new ManagementObjectSearcher("SELECT Name,PNPDeviceID,AdapterRAM,DriverVersion FROM Win32_VideoController"))
        {
            foreach (ManagementObject item in searcher.Get())
            {
                var name = Convert.ToString(item["Name"])?.Trim() ?? "Unknown GPU";
                var pnp = Convert.ToString(item["PNPDeviceID"]) ?? "";
                var pid = ParseDeviceId(pnp);
                var vendor = ParseVendor(pnp, name);
                var driver = Convert.ToString(item["DriverVersion"]) ?? "";
                var ram = ConvertToUInt64(item["AdapterRAM"]);
                gpus.Add(new GpuInfo(name, vendor, pid, pnp, ram, driver));
            }
        }

        return new HardwareInfo(new CpuInfo(cpuName), gpus);
    }

    private static string ParseDeviceId(string pnp)
    {
        var match = Regex.Match(pnp, "DEV_([0-9A-Fa-f]{4})");
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
    }

    private static string ParseVendor(string pnp, string name)
    {
        var match = Regex.Match(pnp, "VEN_([0-9A-Fa-f]{4})");
        if (match.Success)
        {
            return match.Groups[1].Value.ToUpperInvariant() switch
            {
                "10DE" => "NVIDIA",
                "1002" or "1022" => "AMD",
                "8086" => "Intel",
                _ => "Unknown"
            };
        }

        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
        return "Unknown";
    }

    private static ulong ConvertToUInt64(object? value)
    {
        if (value is null) return 0;
        try
        {
            return Convert.ToUInt64(value);
        }
        catch
        {
            return 0;
        }
    }
}

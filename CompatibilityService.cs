using Microsoft.Win32;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MetaLinkCompatTool;

public sealed class CompatibilityPatchResult
{
    public BackupSet Backup { get; init; } = new();
    public List<string> Messages { get; } = new();
}

public sealed class CompatibilityFileStatus
{
    public string Path { get; init; } = "";
    public bool Exists { get; init; }
    public bool ReadOnly { get; init; }
    public bool HasGpuWhiteList { get; init; }
    public bool HasGpuMinSpec { get; init; }
    public bool HasCpuWhiteList { get; init; }
    public bool HasCpuMinSpec { get; init; }
    public DateTime? LastWriteTime { get; init; }
}

public static class CompatibilityService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CompatibilityPatchResult Apply(CpuInfo cpu, GpuInfo gpu, bool lockLocalCompatibilityFile, bool setEncoderDefaults)
    {
        if (string.IsNullOrWhiteSpace(cpu.Name))
        {
            throw new InvalidOperationException("CPU name is empty.");
        }

        if (string.IsNullOrWhiteSpace(gpu.Name) || string.IsNullOrWhiteSpace(gpu.Pid))
        {
            throw new InvalidOperationException("GPU name or PID is empty.");
        }

        var targets = new[] { MetaPaths.LocalCompatibilityJson, MetaPaths.RuntimeCompatibilityJson };
        var backup = BackupStore.CreateSnapshot(cpu, gpu, targets);
        var result = new CompatibilityPatchResult { Backup = backup };

        foreach (var path in targets)
        {
            if (!File.Exists(path))
            {
                result.Messages.Add($"Missing compatibility file: {path}");
                continue;
            }

            var changed = PatchCompatibilityJson(path, cpu, gpu);
            result.Messages.Add(changed ? $"Patched {path}" : $"Entries already present in {path}");

            if (lockLocalCompatibilityFile && string.Equals(path, MetaPaths.LocalCompatibilityJson, StringComparison.OrdinalIgnoreCase))
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                result.Messages.Add("Local Compatibility.json marked read-only.");
            }
        }

        if (setEncoderDefaults)
        {
            SetEncoderDefaults();
            result.Messages.Add("Set Link encoder defaults: HEVC=1, BitrateMbps=0, EncodeWidth=0, DBR=0.");
        }

        return result;
    }

    public static void SetEncoderDefaults()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Oculus\RemoteHeadset", writable: true);
        if (key is null)
        {
            throw new InvalidOperationException("Could not open HKCU\\Software\\Oculus\\RemoteHeadset.");
        }

        key.SetValue("HEVC", 1, RegistryValueKind.DWord);
        key.SetValue("BitrateMbps", 0, RegistryValueKind.DWord);
        key.SetValue("EncodeWidth", 0, RegistryValueKind.DWord);
        key.SetValue("DBR", 0, RegistryValueKind.DWord);
    }

    public static IReadOnlyList<CompatibilityFileStatus> GetStatus(CpuInfo cpu, GpuInfo gpu)
    {
        return new[]
        {
            GetFileStatus(MetaPaths.LocalCompatibilityJson, cpu, gpu),
            GetFileStatus(MetaPaths.RuntimeCompatibilityJson, cpu, gpu)
        };
    }

    public static string GetEncoderStatus()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Oculus\RemoteHeadset", writable: false);
        if (key is null)
        {
            return "RemoteHeadset registry key not found.";
        }

        return $"HEVC={key.GetValue("HEVC", "unset")}  BitrateMbps={key.GetValue("BitrateMbps", "unset")}  EncodeWidth={key.GetValue("EncodeWidth", "unset")}  DBR={key.GetValue("DBR", "unset")}";
    }

    private static bool PatchCompatibilityJson(string path, CpuInfo cpu, GpuInfo gpu)
    {
        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
        var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException($"Invalid JSON: {path}");

        var changed = false;
        changed |= EnsureGpuEntry(root, "VideoCardWhiteList", gpu, minSpec: false);
        changed |= EnsureGpuEntry(root, "VideoCardMinSpecList", gpu, minSpec: true);
        changed |= EnsureCpuEntry(root, "CpuWhiteList", cpu);
        changed |= EnsureCpuEntry(root, "CpuMinSpecList", cpu);

        if (changed)
        {
            File.WriteAllText(path, root.ToJsonString(JsonOptions));
        }

        return changed;
    }

    private static CompatibilityFileStatus GetFileStatus(string path, CpuInfo cpu, GpuInfo gpu)
    {
        if (!File.Exists(path))
        {
            return new CompatibilityFileStatus { Path = path, Exists = false };
        }

        var info = new FileInfo(path);
        var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
        return new CompatibilityFileStatus
        {
            Path = path,
            Exists = true,
            ReadOnly = info.IsReadOnly,
            LastWriteTime = info.LastWriteTime,
            HasGpuWhiteList = HasGpuEntry(root, "VideoCardWhiteList", gpu),
            HasGpuMinSpec = HasGpuEntry(root, "VideoCardMinSpecList", gpu),
            HasCpuWhiteList = HasCpuEntry(root, "CpuWhiteList", cpu),
            HasCpuMinSpec = HasCpuEntry(root, "CpuMinSpecList", cpu)
        };
    }

    private static JsonArray EnsureArray(JsonObject root, string name)
    {
        if (root[name] is JsonArray array)
        {
            return array;
        }

        array = new JsonArray();
        root[name] = array;
        return array;
    }

    private static bool EnsureGpuEntry(JsonObject root, string property, GpuInfo gpu, bool minSpec)
    {
        var array = EnsureArray(root, property);
        if (HasGpuEntry(root, property, gpu))
        {
            return false;
        }

        var item = new JsonObject
        {
            ["Name"] = gpu.Name,
            ["Vendor"] = gpu.Vendor,
            ["PID"] = gpu.Pid.ToUpperInvariant(),
            ["SubsysVID"] = "Any"
        };

        if (minSpec)
        {
            item["MinVRamMb"] = 4095;
            item["Capability"] = new JsonArray(1);
        }

        array.Add(item);
        return true;
    }

    private static bool EnsureCpuEntry(JsonObject root, string property, CpuInfo cpu)
    {
        var array = EnsureArray(root, property);
        if (HasCpuEntry(root, property, cpu))
        {
            return false;
        }

        array.Add(new JsonObject { ["NameSubStr"] = cpu.Name });
        return true;
    }

    private static bool HasGpuEntry(JsonObject? root, string property, GpuInfo gpu)
    {
        if (root?[property] is not JsonArray array)
        {
            return false;
        }

        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            var pid = obj["PID"]?.GetValue<string>();
            var vendor = obj["Vendor"]?.GetValue<string>();
            if (string.Equals(pid, gpu.Pid, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(vendor, gpu.Vendor, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCpuEntry(JsonObject? root, string property, CpuInfo cpu)
    {
        if (root?[property] is not JsonArray array)
        {
            return false;
        }

        foreach (var node in array)
        {
            if (node is not JsonObject obj) continue;
            var name = obj["NameSubStr"]?.GetValue<string>();
            if (string.Equals(name, cpu.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

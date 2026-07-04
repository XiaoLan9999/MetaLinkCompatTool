using System.Text.Json.Serialization;

namespace MetaLinkCompatTool;

public sealed record CpuInfo(string Name);

public sealed record GpuInfo(
    string Name,
    string Vendor,
    string Pid,
    string PnpDeviceId,
    ulong AdapterRamBytes,
    string DriverVersion)
{
    public override string ToString()
    {
        var pid = string.IsNullOrWhiteSpace(Pid) ? "unknown PID" : $"PID {Pid}";
        return $"{Name} ({Vendor}, {pid})";
    }
}

public sealed record HardwareInfo(CpuInfo Cpu, IReadOnlyList<GpuInfo> Gpus);

public sealed class BackupSet
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CpuName { get; set; } = "";
    public string GpuName { get; set; } = "";
    public string GpuVendor { get; set; } = "";
    public string GpuPid { get; set; } = "";
    public List<FileBackup> Files { get; set; } = new();
    public string? RegistryBackupPath { get; set; }

    [JsonIgnore]
    public string DisplayName => $"{CreatedAt:yyyy-MM-dd HH:mm:ss}  {GpuName}  {GpuPid}";
}

public sealed class FileBackup
{
    public string TargetPath { get; set; } = "";
    public string BackupPath { get; set; } = "";
    public bool WasReadOnly { get; set; }
}

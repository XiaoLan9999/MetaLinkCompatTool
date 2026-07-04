using System.Text.Json;

namespace MetaLinkCompatTool;

public static class BackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static IReadOnlyList<BackupSet> Load()
    {
        if (!File.Exists(MetaPaths.BackupManifestPath))
        {
            return Array.Empty<BackupSet>();
        }

        var text = File.ReadAllText(MetaPaths.BackupManifestPath);
        return JsonSerializer.Deserialize<List<BackupSet>>(text, JsonOptions) ?? new List<BackupSet>();
    }

    public static void Save(IEnumerable<BackupSet> sets)
    {
        Directory.CreateDirectory(MetaPaths.BackupRoot);
        File.WriteAllText(MetaPaths.BackupManifestPath, JsonSerializer.Serialize(sets, JsonOptions));
    }

    public static BackupSet CreateSnapshot(CpuInfo cpu, GpuInfo gpu, IEnumerable<string> targetFiles)
    {
        Directory.CreateDirectory(MetaPaths.BackupRoot);
        var id = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(MetaPaths.BackupRoot, id);
        Directory.CreateDirectory(dir);

        var backup = new BackupSet
        {
            Id = id,
            CreatedAt = DateTime.Now,
            CpuName = cpu.Name,
            GpuName = gpu.Name,
            GpuVendor = gpu.Vendor,
            GpuPid = gpu.Pid
        };

        foreach (var path in targetFiles.Where(File.Exists))
        {
            var item = new FileInfo(path);
            var backupPath = Path.Combine(dir, SafeFileName(path) + ".bak");
            File.Copy(path, backupPath, overwrite: true);
            backup.Files.Add(new FileBackup
            {
                TargetPath = path,
                BackupPath = backupPath,
                WasReadOnly = item.IsReadOnly
            });
        }

        var regPath = Path.Combine(dir, "Oculus_RemoteHeadset.reg");
        if (RegistryExport(@"HKCU\Software\Oculus\RemoteHeadset", regPath))
        {
            backup.RegistryBackupPath = regPath;
        }

        var all = Load().ToList();
        all.Insert(0, backup);
        Save(all);
        return backup;
    }

    public static void Restore(BackupSet set)
    {
        foreach (var file in set.Files)
        {
            if (!File.Exists(file.BackupPath))
            {
                continue;
            }

            var targetDir = Path.GetDirectoryName(file.TargetPath);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (File.Exists(file.TargetPath))
            {
                File.SetAttributes(file.TargetPath, File.GetAttributes(file.TargetPath) & ~FileAttributes.ReadOnly);
            }

            File.Copy(file.BackupPath, file.TargetPath, overwrite: true);
            var attrs = File.GetAttributes(file.TargetPath);
            attrs = file.WasReadOnly ? attrs | FileAttributes.ReadOnly : attrs & ~FileAttributes.ReadOnly;
            File.SetAttributes(file.TargetPath, attrs);
        }

        if (!string.IsNullOrWhiteSpace(set.RegistryBackupPath) && File.Exists(set.RegistryBackupPath))
        {
            RegistryImport(set.RegistryBackupPath);
        }
    }

    private static bool RegistryExport(string key, string path)
    {
        var result = ProcessRunner.Run("reg.exe", $"export \"{key}\" \"{path}\" /y", TimeSpan.FromSeconds(10));
        return result.ExitCode == 0 && File.Exists(path);
    }

    private static void RegistryImport(string path)
    {
        var result = ProcessRunner.Run("reg.exe", $"import \"{path}\"", TimeSpan.FromSeconds(10));
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output);
        }
    }

    private static string SafeFileName(string path)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            path = path.Replace(c, '_');
        }

        return path.Replace(':', '_').Replace('\\', '_').Replace('/', '_').Trim('_');
    }
}

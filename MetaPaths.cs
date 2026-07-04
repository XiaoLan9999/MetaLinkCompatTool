namespace MetaLinkCompatTool;

public static class MetaPaths
{
    public static string MetaRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "Meta Horizon");

    public static string LocalOculus => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Oculus");

    public static string LocalCompatibilityJson => Path.Combine(LocalOculus, "Compatibility.json");

    public static string RuntimeCompatibilityJson => Path.Combine(
        MetaRoot,
        "Support",
        "oculus-runtime",
        "Compatibility.json");

    public static string HighwindExe => Path.Combine(
        MetaRoot,
        "Support",
        "oculus-highwind",
        "highwind_service.exe");

    public static string OculusClientExe => Path.Combine(
        MetaRoot,
        "Support",
        "oculus-client",
        "OculusClient.exe");

    public static string AppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MetaLinkCompatTool");

    public static string BackupRoot => Path.Combine(AppDataRoot, "backups");

    public static string BackupManifestPath => Path.Combine(BackupRoot, "manifest.json");
}

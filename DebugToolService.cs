using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;

namespace MetaLinkCompatTool;

public sealed record LinkSettings(bool Hevc, int BitrateMbps, int EncodeWidth, bool DynamicBitrate);

public static class DebugToolService
{
    private const string RemoteHeadsetKeyPath = @"Software\Oculus\RemoteHeadset";

    public static LinkSettings HighRefreshPreset => new(
        Hevc: true,
        BitrateMbps: 500,
        EncodeWidth: 0,
        DynamicBitrate: false);

    public static LinkSettings SafeDefaults => new(
        Hevc: true,
        BitrateMbps: 0,
        EncodeWidth: 0,
        DynamicBitrate: false);

    public static LinkSettings ReadLinkSettings()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RemoteHeadsetKeyPath, writable: false);
        return new LinkSettings(
            Hevc: ReadDWord(key, "HEVC", 1) != 0,
            BitrateMbps: ReadDWord(key, "BitrateMbps", 0),
            EncodeWidth: ReadDWord(key, "EncodeWidth", 0),
            DynamicBitrate: ReadDWord(key, "DBR", 0) != 0);
    }

    public static string GetLinkSettingsStatus()
    {
        var settings = ReadLinkSettings();
        return $"HEVC={(settings.Hevc ? 1 : 0)}  BitrateMbps={settings.BitrateMbps}  EncodeWidth={settings.EncodeWidth}  DBR={(settings.DynamicBitrate ? 1 : 0)}";
    }

    public static string ApplyLinkSettings(LinkSettings settings)
    {
        var backup = BackupRemoteHeadsetRegistry();
        using var key = Registry.CurrentUser.CreateSubKey(RemoteHeadsetKeyPath, writable: true)
            ?? throw new InvalidOperationException(@"Could not open HKCU\Software\Oculus\RemoteHeadset.");

        key.SetValue("HEVC", settings.Hevc ? 1 : 0, RegistryValueKind.DWord);
        key.SetValue("BitrateMbps", Clamp(settings.BitrateMbps, 0, 960), RegistryValueKind.DWord);
        key.SetValue("EncodeWidth", Clamp(settings.EncodeWidth, 0, 5000), RegistryValueKind.DWord);
        key.SetValue("DBR", settings.DynamicBitrate ? 1 : 0, RegistryValueKind.DWord);
        return backup;
    }

    public static void LaunchOculusDebugTool()
    {
        if (!File.Exists(MetaPaths.OculusDebugToolExe))
        {
            throw new FileNotFoundException("OculusDebugTool.exe was not found.", MetaPaths.OculusDebugToolExe);
        }

        Process.Start(new ProcessStartInfo(MetaPaths.OculusDebugToolExe)
        {
            WorkingDirectory = Path.GetDirectoryName(MetaPaths.OculusDebugToolExe),
            UseShellExecute = true
        });
    }

    public static string RunRuntimeCommands(decimal pixelsPerDisplayPixelOverride, string aswMode, int outputColorSpace)
    {
        var commands = new List<string>();
        if (pixelsPerDisplayPixelOverride > 0)
        {
            commands.Add("service set-pixels-per-display-pixel-override " +
                pixelsPerDisplayPixelOverride.ToString("0.##", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(aswMode) && !string.Equals(aswMode, "nochange", StringComparison.OrdinalIgnoreCase))
        {
            commands.Add($"server:asw.OverrideFocusedApp {aswMode}");
        }

        if (outputColorSpace >= 0)
        {
            commands.Add($"server:setOutputColorSpace {outputColorSpace}");
        }

        if (commands.Count == 0)
        {
            return "No Debug Tool runtime command selected.";
        }

        return RunOculusDebugToolCli(commands);
    }

    public static string RunOculusDebugToolCli(IEnumerable<string> commands)
    {
        if (!File.Exists(MetaPaths.OculusDebugToolCliExe))
        {
            throw new FileNotFoundException("OculusDebugToolCLI.exe was not found.", MetaPaths.OculusDebugToolCliExe);
        }

        var commandFile = Path.Combine(Path.GetTempPath(), $"MetaLinkCompatTool-odt-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllLines(commandFile, commands.Concat(["exit"]));
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(MetaPaths.OculusDebugToolCliExe, $"-f \"{commandFile}\"")
            {
                WorkingDirectory = Path.GetDirectoryName(MetaPaths.OculusDebugToolCliExe),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            if (!process.WaitForExit(15000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("OculusDebugToolCLI.exe timed out.");
            }

            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(output.Trim().Length == 0
                    ? $"OculusDebugToolCLI.exe exited with code {process.ExitCode}."
                    : output.Trim());
            }

            return output.Trim();
        }
        finally
        {
            try { File.Delete(commandFile); } catch { }
        }
    }

    private static string BackupRemoteHeadsetRegistry()
    {
        var dir = Path.Combine(MetaPaths.BackupRoot, "debugtool-settings");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"Oculus_RemoteHeadset_{DateTime.Now:yyyyMMdd_HHmmss}.reg");
        var result = ProcessRunner.Run("reg.exe", $@"export ""HKCU\{RemoteHeadsetKeyPath}"" ""{path}"" /y", TimeSpan.FromSeconds(10));
        return result.ExitCode == 0 && File.Exists(path) ? path : "";
    }

    private static int ReadDWord(RegistryKey? key, string name, int defaultValue)
    {
        var value = key?.GetValue(name);
        return value switch
        {
            int i => i,
            long l => l > int.MaxValue ? int.MaxValue : l < int.MinValue ? int.MinValue : (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}

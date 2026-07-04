using Microsoft.Win32;
using System.Diagnostics;
using System.ServiceProcess;

namespace MetaLinkCompatTool;

public static class MetaRuntimeService
{
    public const string HighwindRunValueName = "MetaLinkCompatToolHighwind";

    private static readonly string[] MetaProcessNames =
    {
        "OVRServer_x64",
        "OVRServiceLauncher",
        "OVRRedir",
        "OVRLibraryService",
        "OculusClient",
        "OculusDash",
        "MetaQuestRemoteDesktopCompanion",
        "highwind_service",
        "HighwindCrashpadHandler"
    };

    public static string GetRuntimeStatus()
    {
        var serviceStatus = "OVRService: not found";
        try
        {
            using var svc = new ServiceController("OVRService");
            serviceStatus = $"OVRService: {svc.Status}";
        }
        catch
        {
            // Keep default status.
        }

        var server = Process.GetProcessesByName("OVRServer_x64").Select(p => p.Id).ToArray();
        var highwind = Process.GetProcessesByName("highwind_service").Select(p => p.Id).ToArray();
        var runInstalled = IsHighwindAutostartInstalled() ? "installed" : "not installed";
        return $"{serviceStatus}  |  OVRServer_x64: {JoinIds(server)}  |  highwind_service: {JoinIds(highwind)}  |  highwind autostart: {runInstalled}";
    }

    public static void KillMeta()
    {
        TryStopService("OVRService", TimeSpan.FromSeconds(15));

        foreach (var name in MetaProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                catch
                {
                    // Best effort. Some service-owned processes may already be gone.
                }
            }
        }
    }

    public static void RestartMeta()
    {
        KillMeta();
        TryStartService("OVRService", TimeSpan.FromSeconds(20));
        Thread.Sleep(2000);
        StartHighwind();
    }

    public static void StartHighwind()
    {
        if (!File.Exists(MetaPaths.HighwindExe))
        {
            throw new FileNotFoundException("highwind_service.exe was not found.", MetaPaths.HighwindExe);
        }

        if (Process.GetProcessesByName("highwind_service").Length > 0)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(MetaPaths.HighwindExe)
        {
            WorkingDirectory = Path.GetDirectoryName(MetaPaths.HighwindExe),
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    public static void LaunchMetaClient()
    {
        if (!File.Exists(MetaPaths.OculusClientExe))
        {
            throw new FileNotFoundException("OculusClient.exe was not found.", MetaPaths.OculusClientExe);
        }

        Process.Start(new ProcessStartInfo(MetaPaths.OculusClientExe)
        {
            WorkingDirectory = Path.GetDirectoryName(MetaPaths.OculusClientExe),
            UseShellExecute = true
        });
    }

    public static void InstallHighwindAutostart()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        key?.SetValue(HighwindRunValueName, $"\"{exe}\" --start-highwind --quiet", RegistryValueKind.String);
    }

    public static void RemoveHighwindAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue(HighwindRunValueName, throwOnMissingValue: false);
    }

    public static bool IsHighwindAutostartInstalled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: false);
        return key?.GetValue(HighwindRunValueName) is string value && value.Length > 0;
    }

    private static void TryStopService(string name, TimeSpan timeout)
    {
        try
        {
            using var svc = new ServiceController(name);
            if (svc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
            {
                return;
            }

            svc.Stop();
            svc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
        catch
        {
            // Best effort; process kill follows.
        }
    }

    private static void TryStartService(string name, TimeSpan timeout)
    {
        using var svc = new ServiceController(name);
        if (svc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
        {
            return;
        }

        svc.Start();
        svc.WaitForStatus(ServiceControllerStatus.Running, timeout);
    }

    private static string JoinIds(IReadOnlyCollection<int> ids)
    {
        return ids.Count == 0 ? "not running" : string.Join(", ", ids);
    }
}

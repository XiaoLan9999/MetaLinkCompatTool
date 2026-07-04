using System.Diagnostics;
using System.Security.Principal;

namespace MetaLinkCompatTool;

public static class Elevation
{
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RelaunchElevated()
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Verb = "runas"
        };
        Process.Start(psi);
    }
}

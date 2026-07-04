using System.Diagnostics;

namespace MetaLinkCompatTool;

public sealed record ProcessResult(int ExitCode, string Output);

public static class ProcessRunner
{
    public static ProcessResult Run(string fileName, string arguments, TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new ProcessResult(-1, "Process timed out.");
        }

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        return new ProcessResult(process.ExitCode, output);
    }
}

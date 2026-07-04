namespace MetaLinkCompatTool;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--start-highwind", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                MetaRuntimeService.StartHighwind();
            }
            catch
            {
                if (!args.Contains("--quiet", StringComparer.OrdinalIgnoreCase))
                {
                    throw;
                }
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

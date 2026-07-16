namespace MetaLinkCompatTool;

public enum UiThemeKind
{
    Atelier,
    Mist,
    Ink
}

public sealed record UiThemePalette(
    UiThemeKind Kind,
    Color Background,
    Color Surface,
    Color SurfaceAlt,
    Color Accent,
    Color AccentSecondary,
    Color Text,
    Color Muted,
    Color Border,
    Color Warning,
    Color Danger,
    Color Info,
    Color Success,
    Color Lavender,
    Color Aqua,
    Color NavigationActiveBackground,
    Color NavigationActiveText,
    string DisplayFont,
    string BodyFont,
    int CardRadius,
    int ButtonRadius);

public sealed record UiThemeOption(UiThemeKind Kind, string Text)
{
    public override string ToString() => Text;
}

public static class UiThemes
{
    public static UiThemePalette Get(UiThemeKind kind) => kind switch
    {
        UiThemeKind.Mist => new UiThemePalette(
            kind,
            Color.FromArgb(243, 247, 246),
            Color.FromArgb(255, 255, 253),
            Color.FromArgb(230, 237, 234),
            Color.FromArgb(72, 111, 102),
            Color.FromArgb(161, 112, 78),
            Color.FromArgb(40, 53, 50),
            Color.FromArgb(105, 121, 117),
            Color.FromArgb(208, 220, 216),
            Color.FromArgb(178, 132, 70),
            Color.FromArgb(171, 86, 91),
            Color.FromArgb(103, 132, 148),
            Color.FromArgb(105, 143, 111),
            Color.FromArgb(139, 121, 153),
            Color.FromArgb(91, 139, 136),
            Color.FromArgb(255, 255, 253),
            Color.FromArgb(72, 111, 102),
            "Microsoft YaHei UI",
            "Segoe UI",
            16,
            9),
        UiThemeKind.Ink => new UiThemePalette(
            kind,
            Color.FromArgb(16, 20, 28),
            Color.FromArgb(27, 34, 48),
            Color.FromArgb(35, 44, 62),
            Color.FromArgb(92, 150, 255),
            Color.FromArgb(83, 220, 180),
            Color.FromArgb(238, 243, 250),
            Color.FromArgb(155, 166, 184),
            Color.FromArgb(55, 66, 86),
            Color.FromArgb(255, 190, 101),
            Color.FromArgb(255, 110, 120),
            Color.FromArgb(130, 185, 255),
            Color.FromArgb(148, 210, 130),
            Color.FromArgb(185, 145, 255),
            Color.FromArgb(120, 205, 210),
            Color.FromArgb(92, 150, 255),
            Color.White,
            "Segoe UI Semibold",
            "Segoe UI",
            14,
            7),
        _ => new UiThemePalette(
            UiThemeKind.Atelier,
            Color.FromArgb(246, 243, 236),
            Color.FromArgb(255, 253, 248),
            Color.FromArgb(236, 231, 220),
            Color.FromArgb(194, 105, 78),
            Color.FromArgb(102, 134, 110),
            Color.FromArgb(45, 49, 44),
            Color.FromArgb(116, 118, 108),
            Color.FromArgb(222, 215, 202),
            Color.FromArgb(190, 143, 76),
            Color.FromArgb(180, 88, 88),
            Color.FromArgb(103, 133, 151),
            Color.FromArgb(109, 151, 111),
            Color.FromArgb(148, 123, 158),
            Color.FromArgb(95, 145, 145),
            Color.FromArgb(255, 253, 248),
            Color.FromArgb(194, 105, 78),
            "Microsoft YaHei UI",
            "Segoe UI",
            18,
            10)
    };
}

public static class UiThemeSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MetaLinkCompatTool",
        "theme.txt");

    public static UiThemeKind Load()
    {
        var argument = Environment.GetCommandLineArgs()
            .FirstOrDefault(value => value.StartsWith("--theme=", StringComparison.OrdinalIgnoreCase));
        if (argument is not null && Enum.TryParse<UiThemeKind>(argument[8..], true, out var commandLineTheme))
        {
            return commandLineTheme;
        }

        try
        {
            if (File.Exists(SettingsPath) &&
                Enum.TryParse<UiThemeKind>(File.ReadAllText(SettingsPath).Trim(), true, out var savedTheme))
            {
                return savedTheme;
            }
        }
        catch
        {
            // A read-only profile should not prevent the application from starting.
        }

        return UiThemeKind.Atelier;
    }

    public static void Save(UiThemeKind kind)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, kind.ToString());
    }
}

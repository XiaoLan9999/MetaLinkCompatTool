namespace MetaLinkCompatTool;

public sealed record UiThemePalette(
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

public static class UiThemes
{
    public static readonly UiThemePalette Atelier = new(
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
        10);
}

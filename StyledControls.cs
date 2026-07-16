using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MetaLinkCompatTool;

public sealed class RoundedPanel : Panel
{
    public int CornerRadius { get; set; } = 16;
    public int BorderThickness { get; set; } = 1;
    public Color BorderColor { get; set; } = Color.Transparent;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (BorderThickness <= 0 || BorderColor == Color.Transparent || Width <= 1 || Height <= 1)
        {
            return;
        }

        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedPath(new RectangleF(
            BorderThickness / 2F,
            BorderThickness / 2F,
            Width - BorderThickness,
            Height - BorderThickness), CornerRadius);
        using var pen = new Pen(BorderColor, BorderThickness);
        eventArgs.Graphics.DrawPath(pen, path);
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = CreateRoundedPath(new RectangleF(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
    }

    private static GraphicsPath CreateRoundedPath(RectangleF rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(2, Math.Min(radius * 2, (int)Math.Min(rectangle.Width, rectangle.Height)));
        var arc = new RectangleF(rectangle.X, rectangle.Y, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = rectangle.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rectangle.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rectangle.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class RoundedButton : Button
{
    public int CornerRadius { get; set; } = 9;

    public RoundedButton()
    {
        ResizeRedraw = true;
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var radius = Math.Max(2, Math.Min(CornerRadius * 2, Math.Min(Width, Height)));
        using var path = new GraphicsPath();
        path.AddArc(0, 0, radius, radius, 180, 90);
        path.AddArc(Width - radius, 0, radius, radius, 270, 90);
        path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
        path.AddArc(0, Height - radius, radius, radius, 90, 90);
        path.CloseFigure();
        Region?.Dispose();
        Region = new Region(path);
    }
}

public static class WindowChrome
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);

    public static void ApplyDarkTitleBar(IntPtr windowHandle, bool enabled)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            return;
        }

        var value = enabled ? 1 : 0;
        if (DwmSetWindowAttribute(windowHandle, 20, ref value, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(windowHandle, 19, ref value, sizeof(int));
        }
    }
}

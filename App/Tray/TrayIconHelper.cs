namespace LecooHelper.App.Tray;

internal static class TrayIconHelper
{
    public static Icon CreateTemperatureIcon(float maxCelsius)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var accent = TrayTheme.TrayIconColor(maxCelsius);
        using var brush = new SolidBrush(accent);
        graphics.FillEllipse(brush, 4, 4, size - 8, size - 8);

        using var inner = new SolidBrush(TrayTheme.Background);
        graphics.FillEllipse(inner, 10, 10, size - 20, size - 20);

        var hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        return (Icon)icon.Clone();
    }

    public static void DisposeIcon(Icon? icon)
    {
        if (icon is null)
            return;

        icon.Dispose();
    }
}

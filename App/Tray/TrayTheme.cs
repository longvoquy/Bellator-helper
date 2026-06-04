namespace LecooHelper.App.Tray;

internal static class TrayTheme
{
    public static readonly Color Background = Color.FromArgb(10, 14, 26);
    public static readonly Color Surface = Color.FromArgb(30, 40, 60);
    public static readonly Color Border = Color.FromArgb(52, 58, 78);
    public static readonly Color Text = Color.FromArgb(0, 212, 255);
    public static readonly Color TextMuted = Color.FromArgb(120, 140, 170);
    public static readonly Color GaugeTrack = Color.FromArgb(30, 40, 60);
    public static readonly Color GaugeFill = Color.FromArgb(0, 212, 255);
    public static readonly Color GaugeWarn = Color.FromArgb(255, 193, 7);
    public static readonly Color GaugeHot = Color.FromArgb(255, 82, 82);
    public static readonly Color ModeActiveFill = Color.FromArgb(30, 0, 140, 255);
    public static readonly Color HeaderHover = Color.FromArgb(50, 58, 78);

    public static Color TempAccent(float celsius)
    {
        if (celsius <= 0f)
            return TextMuted;
        if (celsius < 70f)
            return Color.FromArgb(76, 175, 80);
        if (celsius <= 85f)
            return GaugeWarn;
        return GaugeHot;
    }

    public static Color TrayIconColor(float celsius) => TempAccent(celsius <= 0f ? 0f : celsius);
}

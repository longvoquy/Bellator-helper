using BHelper.App.Power;

namespace BHelper.App.Tray;

internal static class TrayTheme
{
    public const string FontFamily = "Segoe UI";

    public static readonly Font Title = new(FontFamily, 12f, FontStyle.Bold);
    public static readonly Font CloseButton = new(FontFamily, 10f);
    public static readonly Font SectionLabel = new(FontFamily, 9f, FontStyle.Bold);
    public static readonly Font Body = new(FontFamily, 9f);
    public static readonly Font StatValue = new(FontFamily, 11f, FontStyle.Bold);
    public static readonly Font ValueLarge = new(FontFamily, 18f, FontStyle.Bold);
    public static readonly Font ValueSmall = new(FontFamily, 11f, FontStyle.Bold);

    public static readonly Color Background = Color.FromArgb(10, 14, 26);
    public static readonly Color Surface = Color.FromArgb(30, 40, 60);
    public static readonly Color Border = Color.White;
    public static readonly Color Text = Color.White;
    public static readonly Color TextMuted = Color.White;
    public static readonly Color GaugeWarn = Color.FromArgb(255, 193, 7);
    public static readonly Color GaugeHot = Color.FromArgb(255, 82, 82);

    public static readonly Color ModeSilentFill = Color.FromArgb(100, 60, 255);
    public static readonly Color ModeBalancedFill = Color.FromArgb(255, 255, 255);
    public static readonly Color ModeBeastFill = Color.FromArgb(195, 142, 50);
    public static readonly Color ModeBattleFill = Color.FromArgb(225, 37, 27);
    public static readonly Color ModeButtonBorder = Color.FromArgb(45, 55, 75);
    public static readonly Color ModeButtonSelectedBorder = ModeBattleFill;

    public static Color ModeFill(PowerModeKind kind) => kind switch
    {
        PowerModeKind.Silent => ModeSilentFill,
        PowerModeKind.Balanced => ModeBalancedFill,
        PowerModeKind.Beast => ModeBeastFill,
        PowerModeKind.Battle => ModeBattleFill,
        _ => Surface
    };

    public static Color ModeForeColor(PowerModeKind kind) => Text;

    public static Color TempAccent(float celsius)
    {
        if (celsius <= 0f)
            return Text;
        if (celsius < 70f)
            return Color.FromArgb(76, 175, 80);
        if (celsius <= 85f)
            return GaugeWarn;
        return GaugeHot;
    }

    public static Color TrayIconColor(float celsius) => TempAccent(celsius <= 0f ? 0f : celsius);
}

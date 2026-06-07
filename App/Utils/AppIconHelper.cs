namespace BHelper.App.Utils;

internal static class AppIconHelper
{
    private const string IconFileName = "gcc.ico";

    public static Icon CreateTrayIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", IconFileName);
        using var source = new Icon(path);
        return new Icon(source, SystemInformation.SmallIconSize);
    }

    public static void DisposeIcon(Icon? icon) => icon?.Dispose();
}

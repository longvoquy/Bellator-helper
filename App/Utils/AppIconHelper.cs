namespace BHelper.App.Utils;

internal static class AppIconHelper
{
    private const string IconFileName = "gcc.ico";

    public static Icon CreateTrayIcon()
    {
        using var source = LoadSourceIcon();
        return new Icon(source, SystemInformation.SmallIconSize);
    }

    private static Icon LoadSourceIcon()
    {
        // ApplicationIcon embeds gcc.ico in the exe; publish output often omits the loose file.
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            var fromExe = Icon.ExtractAssociatedIcon(exePath);
            if (fromExe is not null)
                return fromExe;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "Resources", IconFileName);
        return new Icon(path);
    }

    public static void DisposeIcon(Icon? icon) => icon?.Dispose();
}

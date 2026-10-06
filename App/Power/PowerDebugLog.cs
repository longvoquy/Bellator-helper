namespace BHelper.App.Power;

// Off by default. Enabled from Settings.EnablePowerDebugLog; writes logs/power_debug.txt under the app data folder.
public static class PowerDebugLog
{
    private static readonly object Sync = new();
    private static string? _filePath;

    public static void Enable(string dataDirectory)
    {
        var logDirectory = Path.Combine(dataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        _filePath = Path.Combine(logDirectory, "power_debug.txt");
    }

    public static void Write(string message)
    {
        var path = _filePath;
        if (path is null)
            return;

        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Logging must never break power switching.
        }
    }
}

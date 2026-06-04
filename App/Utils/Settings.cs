using System.Text.Json;

namespace LecooHelper.App.Utils;

public sealed class Settings
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LecooHelper");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    public int UpdateIntervalMs { get; set; } = 2000;
    public bool StartMinimized { get; set; } = true;
    public bool AutoStart { get; set; }
    public string DefaultPowerMode { get; set; } = "Balanced";
    public bool ShowInTray { get; set; } = true;

    public static Settings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new Settings();

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFilePath, json);
    }
}

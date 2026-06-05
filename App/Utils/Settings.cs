using System.Text.Json;
using System.Text.Json.Serialization;
using LecooHelper.App.Power;

namespace LecooHelper.App.Utils;

public sealed class Settings
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LecooHelper");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new PowerModeKindJsonConverter() }
    };

    public int UpdateIntervalMs { get; set; } = 2000;
    public bool StartMinimized { get; set; } = true;
    public bool AutoStart { get; set; }

    [JsonConverter(typeof(PowerModeKindJsonConverter))]
    public PowerModeKind DefaultPowerMode { get; set; } = PowerModeKind.Balanced;

    public bool ShowInTray { get; set; } = true;

    public static Settings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new Settings();

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }
}

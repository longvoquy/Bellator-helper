using System.Text.Json;
using System.Text.Json.Serialization;
using BHelper.App.Power;

namespace BHelper.App.Utils;

public sealed class Settings
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppBranding.AppId);

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    public static string DataDirectory => SettingsDirectory;

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

    public bool EnablePowerDebugLog { get; set; }

    // Manual max-RPM cap for the fans. Off by default: the firmware controls the fans.
    // Values are hundreds of RPM (24 = 2400 RPM), stored per power mode name (Silent, Balanced, ...).
    public bool FanLimitEnabled { get; set; }
    public Dictionary<string, int> FanCpuGpuLimits { get; set; } = new();
    public Dictionary<string, int> FanSysLimits { get; set; } = new();

    // Last automatic update check (UTC). The app checks at most once per day.
    public DateTime? LastUpdateCheckUtc { get; set; }

    // Also logs raw Bellator Fn-key WMI events to logs/hid_events.txt (diagnostic). Fn+F10 itself always works.
    public bool EnableHidEventLog { get; set; }

    // Last mode the user picked while on AC / on battery. Auto-switch restores these; no UI.
    [JsonConverter(typeof(PowerModeKindJsonConverter))]
    public PowerModeKind AcPowerMode { get; set; } = PowerModeKind.Balanced;

    [JsonConverter(typeof(PowerModeKindJsonConverter))]
    public PowerModeKind BatteryPowerMode { get; set; } = PowerModeKind.Silent;

    // Win32 MOD_* flags: MOD_CONTROL (0x0002) | MOD_ALT (0x0001)
    public int HotkeyModifiers { get; set; } = 0x0003;

    // Virtual-key code: 'P'. Do not use 0xFF: the Fn key alone also sends 0xFF on Bellator laptops.
    public int HotkeyKey { get; set; } = 0x50;

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

namespace BHelper.App.Tray;

internal readonly record struct HardwareSnapshot(
    float CpuTemp,
    float GpuTemp,
    int CpuFanRpm,
    int GpuFanRpm,
    int SysFanRpm,
    float RamUsagePercent,
    float RamUsedGb,
    float RamTotalGb,
    bool HasBattery,
    int BatteryPercent,
    bool OnAcPower,
    bool IsCharging,
    int BatteryHealthPercent,
    int BatteryCycles)
{
    public float MaxTemp => Math.Max(CpuTemp, GpuTemp);

    // Plain wording: "Plugged in" means the charger is connected, even when the battery takes no charge.
    public string BatteryStateText =>
        !OnAcPower ? "On battery" : IsCharging ? "Charging" : "Plugged in";

    // First line is the title, the rest is the description.
    public string BatteryTooltip
    {
        get
        {
            if (!HasBattery)
                return "No battery\nNo battery was detected.";

            if (!OnAcPower)
                return "On battery\nThe charger is not connected.";

            if (IsCharging)
                return "Charging\nThe charger is connected and the battery is filling up.";

            return BatteryPercent >= 95
                ? "Plugged in\nBattery is full, so it is not charging. This is normal."
                : "Plugged in\nThe charger is connected, but the battery is not charging right now.";
        }
    }
}

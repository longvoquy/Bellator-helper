namespace BHelper.App.Tray;

internal readonly record struct HardwareSnapshot(
    float CpuTemp,
    float GpuTemp,
    int CpuFanRpm,
    int GpuFanRpm,
    float RamUsagePercent,
    float RamUsedGb,
    float RamTotalGb)
{
    public float MaxTemp => Math.Max(CpuTemp, GpuTemp);
}

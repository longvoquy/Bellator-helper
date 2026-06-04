namespace LecooHelper.App.Tray;

internal readonly record struct HardwareSnapshot(float CpuTemp, float GpuTemp)
{
    public float MaxTemp => Math.Max(CpuTemp, GpuTemp);
}

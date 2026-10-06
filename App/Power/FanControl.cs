using System.Management;

namespace BHelper.App.Power;

public enum FanKind : byte
{
    CpuGpu = 0,
    Sys = 1
}

// Allowed range of the max-RPM cap for one fan in one mode. Values are hundreds of RPM (24 = 2400 RPM).
// The default is the top of the range, i.e. the least restrictive cap (same as the vendor tool shows).
public readonly record struct FanLimitRange(int Min, int Max)
{
    public int Default => Max;

    public int Clamp(int value) => Math.Clamp(value, Min, Max);
}

// Manual max-RPM cap for the two fans. The ranges are the manufacturer's own, per performance mode;
// values are always clamped into them, so the app cannot ask for anything the vendor tool would not.
public static class FanControl
{
    public const int RpmPerUnit = 100;

    private static readonly object Sync = new();
    private static bool? _isAmd;

    public static int ToRpm(int value) => value * RpmPerUnit;

    public static FanLimitRange GetRange(PowerModeKind mode, FanKind fan) =>
        fan == FanKind.CpuGpu ? CpuGpuRange(mode) : IsAmd() ? SysRangeAmd(mode) : SysRangeIntel(mode);

    // Mode names follow the vendor tool: Silent = Work, Balanced = Playing, Beast = Fastest, Battle = Fullspeed.
    private static FanLimitRange CpuGpuRange(PowerModeKind mode) => mode switch
    {
        PowerModeKind.Silent => new(19, 29),
        PowerModeKind.Balanced => new(26, 35),
        PowerModeKind.Beast => new(32, 38),
        _ => new(40, 44)
    };

    private static FanLimitRange SysRangeIntel(PowerModeKind mode) => mode switch
    {
        PowerModeKind.Silent => new(25, 64),
        PowerModeKind.Balanced => new(59, 69),
        PowerModeKind.Beast => new(70, 80),
        _ => new(75, 82)
    };

    private static FanLimitRange SysRangeAmd(PowerModeKind mode) => mode switch
    {
        PowerModeKind.Silent => new(17, 64),
        PowerModeKind.Balanced => new(59, 69),
        PowerModeKind.Beast => new(64, 72),
        _ => new(75, 82)
    };

    // Applies the cap for the given mode, or hands both fans back to the firmware when disabled.
    // Slow (WMI): call from a background thread.
    public static bool TryApply(PowerModeKind mode, bool enabled, int cpuGpuValue, int sysValue)
    {
        lock (Sync)
        {
            var ok = true;
            ok &= ApplyOne(FanKind.CpuGpu, enabled, GetRange(mode, FanKind.CpuGpu).Clamp(cpuGpuValue));
            ok &= ApplyOne(FanKind.Sys, enabled, GetRange(mode, FanKind.Sys).Clamp(sysValue));

            PowerDebugLog.Write(enabled
                ? $"fan limit {mode}: cpu/gpu {cpuGpuValue}, sys {sysValue}, ok={ok}"
                : $"fan limit off, ok={ok}");
            return ok;
        }
    }

    private static bool ApplyOne(FanKind fan, bool enabled, int value)
    {
        var type = (byte)fan;
        if (!enabled)
            return BellatorWmiClient.TrySetMaxFanSwitch(type, false);

        // Same order as the vendor tool: switch on, then the value.
        return BellatorWmiClient.TrySetMaxFanSwitch(type, true)
               && BellatorWmiClient.TrySetMaxFanSpeed(type, (byte)value);
    }

    private static bool IsAmd()
    {
        if (_isAmd is { } known)
            return known;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                _isAmd = obj["Name"]?.ToString()?.Contains("AMD", StringComparison.OrdinalIgnoreCase) == true;
                return _isAmd.Value;
            }
        }
        catch
        {
            // Fall through to the Intel table.
        }

        _isAmd = false;
        return false;
    }
}

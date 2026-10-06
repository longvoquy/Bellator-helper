using System.Windows.Forms;

namespace BHelper.App.Power;

public enum PowerModeKind
{
    Silent,
    Balanced,
    Beast,
    Battle
}

public static class PowerMode
{
    public static PowerModeKind Current { get; private set; } = PowerModeKind.Balanced;

    public static bool TrySyncFromSystem()
    {
        if (!BellatorWmiClient.TryGetSystemPerMode(out var wmiMode))
        {
            PowerDebugLog.Write("sync from system: WMI read failed");
            return false;
        }

        Current = BellatorWmiClient.ToPowerModeKind(wmiMode);
        PowerDebugLog.Write($"sync from system: {Current}");
        return true;
    }

    // Mode reported by the firmware event (raw WMI value). Returns true when it differs from Current.
    public static bool TryApplyReportedMode(byte rawMode)
    {
        if (rawMode > (byte)BellatorSystemPerMode.FullspeedMode)
            return false;

        var kind = BellatorWmiClient.ToPowerModeKind((BellatorSystemPerMode)rawMode);
        if (kind == Current)
            return false;

        Current = kind;
        PowerDebugLog.Write($"reported by firmware: {kind}");
        return true;
    }

    public static bool SetMode(PowerModeKind mode)
    {
        var wmiMode = BellatorWmiClient.FromPowerModeKind(mode);
        if (!BellatorWmiClient.TrySetSystemPerMode(wmiMode))
        {
            PowerDebugLog.Write($"set {mode}: WMI write failed");
            return false;
        }

        Current = mode;
        PowerDebugLog.Write($"set {mode}: ok");
        return true;
    }

    public static PowerModeKind Next(PowerModeKind mode)
    {
        var profiles = PerformanceProfile.All;
        var index = profiles.ToList().FindIndex(p => p.Kind == mode);
        return profiles[(index + 1) % profiles.Count].Kind;
    }

    public static bool IsOnAcPower() =>
        SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Offline;}

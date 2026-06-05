namespace LecooHelper.App.Power;

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
            return false;

        Current = BellatorWmiClient.ToPowerModeKind(wmiMode);
        return true;
    }

    public static bool SetMode(PowerModeKind mode)
    {
        var wmiMode = BellatorWmiClient.FromPowerModeKind(mode);
        if (!BellatorWmiClient.TrySetSystemPerMode(wmiMode))
            return false;

        Current = mode;
        return true;
    }
}

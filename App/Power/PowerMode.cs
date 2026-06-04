namespace LecooHelper.App.Power;

public enum PowerModeKind
{
    Performance,
    Balanced,
    Silent,
    Turbo
}

public static class PowerMode
{
    public static PowerModeKind Current { get; private set; } = PowerModeKind.Balanced;

    public static bool SetMode(PowerModeKind mode)
    {
        Current = mode;
        return false;
    }
}

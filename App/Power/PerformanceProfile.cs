namespace LecooHelper.App.Power;

public sealed class PerformanceProfile
{
    public PerformanceProfile(PowerModeKind kind, string name, string description)
    {
        Kind = kind;
        Name = name;
        Description = description;
    }

    public PowerModeKind Kind { get; }
    public string Name { get; }
    public string Description { get; }

    public static IReadOnlyList<PerformanceProfile> All { get; } =
    [
        new(PowerModeKind.Performance, "Performance", "Maximum performance"),
        new(PowerModeKind.Balanced, "Balanced", "Balanced power and performance"),
        new(PowerModeKind.Silent, "Silent", "Quiet operation, lower power"),
        new(PowerModeKind.Turbo, "Turbo", "Maximum boost")
    ];
}

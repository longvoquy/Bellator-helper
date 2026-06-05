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

    public static PowerModeKind DefaultKind => PowerModeKind.Balanced;

    public static IReadOnlyList<PerformanceProfile> All { get; } =
    [
        new(PowerModeKind.Silent, "Silent", "Quiet operation, lower power consumption"),
        new(PowerModeKind.Balanced, "Balanced", "Balanced power and performance"),
        new(PowerModeKind.Beast, "Beast", "High performance for heavy workloads"),
        new(PowerModeKind.Battle, "Battle", "Maximum boost and cooling for extreme gaming")
    ];

    public static string GetDisplayName(PowerModeKind kind) =>
        All.FirstOrDefault(p => p.Kind == kind)?.Name ?? kind.ToString();
}

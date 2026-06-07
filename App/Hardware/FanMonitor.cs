using System.Management;
using LibreHardwareMonitor.Hardware;

namespace BHelper.App.Hardware;

public sealed class FanMonitor : PollingMonitorBase
{
    private readonly object _sync = new();

    public FanMonitor(int intervalMs = 2000)
        : base(intervalMs)
    {
    }

    public IReadOnlyList<int> RpmValues { get; private set; } = Array.Empty<int>();

    protected override void Refresh()
    {
        var rpms = ReadFromLibreHardwareMonitor();
        if (rpms.Count == 0)
            rpms = ReadFromWmi();

        lock (_sync)
            RpmValues = rpms;
    }

    private static List<int> ReadFromLibreHardwareMonitor()
    {
        var computer = HardwareMonitorHost.Computer;
        if (computer is null)
            return [];

        var fanValues = SensorReader.AllValues(computer, SensorType.Fan);
        return fanValues
            .Where(v => v > 0f)
            .Select(v => (int)Math.Round(v))
            .Distinct()
            .OrderByDescending(v => v)
            .ToList();
    }

    private static List<int> ReadFromWmi()
    {
        var rpms = new List<int>();

        WmiHelper.TryExecute("Win32_Fan", () =>
        {
            using var searcher = new ManagementObjectSearcher("SELECT DesiredSpeed FROM Win32_Fan");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                if (obj["DesiredSpeed"] is uint rpm && rpm > 0)
                    rpms.Add((int)rpm);
            }

            return true;
        });

        return rpms;
    }
}

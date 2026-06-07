using System.Management;

namespace BHelper.App.Hardware;

public sealed class RamMonitor : PollingMonitorBase
{
    private readonly object _sync = new();

    public RamMonitor(int intervalMs = 2000)
        : base(intervalMs)
    {
    }

    public float UsagePercent { get; private set; }
    public float UsedGb { get; private set; }
    public float TotalGb { get; private set; }

    protected override void Refresh()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                if (obj["TotalVisibleMemorySize"] is not ulong totalKb ||
                    obj["FreePhysicalMemory"] is not ulong freeKb)
                    continue;

                var totalGb = totalKb / 1024f / 1024f;
                var usedGb = (totalKb - freeKb) / 1024f / 1024f;
                var usagePercent = totalKb > 0 ? (totalKb - freeKb) * 100f / totalKb : 0f;

                lock (_sync)
                {
                    TotalGb = totalGb;
                    UsedGb = usedGb;
                    UsagePercent = usagePercent;
                }

                return;
            }
        }
        catch
        {
            lock (_sync)
            {
                UsagePercent = 0f;
                UsedGb = 0f;
                TotalGb = 0f;
            }
        }
    }
}

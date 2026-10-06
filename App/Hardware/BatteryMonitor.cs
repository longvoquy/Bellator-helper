using System.Management;

namespace BHelper.App.Hardware;

public sealed class BatteryMonitor : PollingMonitorBase
{
    private readonly object _sync = new();

    public BatteryMonitor(int intervalMs = 30000)
        : base(intervalMs)
    {
    }

    public bool HasBattery { get; private set; }
    public int ChargePercent { get; private set; }
    public bool OnAcPower { get; private set; }

    // Full charge capacity / designed capacity, in percent. 0 when unknown.
    public int HealthPercent { get; private set; }
    public int CycleCount { get; private set; }

    protected override void Refresh()
    {
        try
        {
            if (!TryReadWin32Battery(out var chargePercent, out var batteryStatus))
            {
                SetNoBattery();
                return;
            }

            // BatteryStatus 1 = discharging, 4 = low, 5 = critical. Anything else means AC is connected.
            var onAc = batteryStatus is not (1 or 4 or 5);

            // Health and cycles are read from the battery driver (root\WMI). Missing on some drivers.
            var fullCapacity = QueryFirst(@"root\WMI",
                "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity",
                "FullChargedCapacity") as uint? ?? 0u;
            var designedCapacity = QueryFirst(@"root\WMI",
                "SELECT DesignedCapacity FROM BatteryStaticData",
                "DesignedCapacity") as uint? ?? 0u;
            var cycles = QueryFirst(@"root\WMI",
                "SELECT CycleCount FROM BatteryCycleCount",
                "CycleCount") as uint? ?? 0u;

            var health = designedCapacity > 0
                ? (int)Math.Round(fullCapacity * 100.0 / designedCapacity)
                : 0;

            lock (_sync)
            {
                HasBattery = true;
                ChargePercent = chargePercent;
                OnAcPower = onAc;
                HealthPercent = health;
                CycleCount = (int)cycles;
            }
        }
        catch
        {
            SetNoBattery();
        }
    }

    private void SetNoBattery()
    {
        lock (_sync)
        {
            HasBattery = false;
            ChargePercent = 0;
            OnAcPower = false;
            HealthPercent = 0;
            CycleCount = 0;
        }
    }

    private static bool TryReadWin32Battery(out ushort chargePercent, out ushort batteryStatus)
    {
        chargePercent = 0;
        batteryStatus = 0;

        using var searcher = new ManagementObjectSearcher(
            @"root\cimv2", "SELECT EstimatedChargeRemaining, BatteryStatus FROM Win32_Battery");
        using var results = searcher.Get();

        foreach (var obj in results.Cast<ManagementObject>())
        {
            if (obj["EstimatedChargeRemaining"] is ushort charge && obj["BatteryStatus"] is ushort status)
            {
                chargePercent = charge;
                batteryStatus = status;
                return true;
            }
        }

        return false;
    }

    private static object? QueryFirst(string scope, string query, string property)
    {
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        foreach (var obj in results.Cast<ManagementObject>())
            return obj[property];

        return null;
    }
}

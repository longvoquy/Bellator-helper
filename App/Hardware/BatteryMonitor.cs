using System.Management;

namespace BHelper.App.Hardware;

public sealed class BatteryMonitor : PollingMonitorBase
{
    private readonly object _sync = new();
    private bool _staticInfoRead;

    public BatteryMonitor(int intervalMs = 30000)
        : base(intervalMs)
    {
    }

    protected override bool UsesHardwareHost => false;

    public bool HasBattery { get; private set; }
    public int ChargePercent { get; private set; }
    public bool OnAcPower { get; private set; }

    // True only while current flows into the battery. Plugged in but full or holding is false.
    public bool IsCharging { get; private set; }

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

            // BatteryStatus 6..9 = charging (normal, high, low, critical).
            var isCharging = batteryStatus is >= 6 and <= 9;

            // Health and cycles change very slowly: read them once, and retry only while that has failed.
            if (!_staticInfoRead && BatteryInfoReader.TryRead(out var info))
            {
                _staticInfoRead = true;
                var health = info.DesignedCapacityMwh > 0
                    ? (int)Math.Round(info.FullChargedCapacityMwh * 100.0 / info.DesignedCapacityMwh)
                    : 0;

                lock (_sync)
                {
                    HealthPercent = health;
                    CycleCount = info.CycleCount;
                }
            }

            lock (_sync)
            {
                HasBattery = true;
                ChargePercent = chargePercent;
                OnAcPower = onAc;
                IsCharging = isCharging;
            }
        }
        catch
        {
            SetNoBattery();
        }
    }

    private void SetNoBattery()
    {
        _staticInfoRead = false;
        lock (_sync)
        {
            HasBattery = false;
            ChargePercent = 0;
            OnAcPower = false;
            IsCharging = false;
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
}

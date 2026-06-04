using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace LecooHelper.App.Hardware;

public sealed class CpuMonitor : PollingMonitorBase
{
    private readonly object _sync = new();
    private PerformanceCounter? _usageCounter;

    public CpuMonitor(int intervalMs = 2000)
        : base(intervalMs)
    {
    }

    public float Usage { get; private set; }
    public float Temp { get; private set; }
    public float FreqGhz { get; private set; }

    public new void Start()
    {
        _usageCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _usageCounter.NextValue();
        base.Start();
    }

    public new void Stop()
    {
        base.Stop();
        _usageCounter?.Dispose();
        _usageCounter = null;
    }

    public new void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    protected override void Refresh()
    {
        var usage = _usageCounter?.NextValue() ?? 0f;
        var temp = ReadCpuTemperature();

        lock (_sync)
        {
            Usage = usage;
            Temp = temp;
        }
    }

    private static float ReadCpuTemperature()
    {
        var computer = HardwareMonitorHost.Computer;
        if (computer is null)
            return 0f;

        var fromLhm = SensorReader.BestCpuTemperature(computer);
        if (fromLhm is > 0f)
            return fromLhm.Value;

        return ReadCpuTemperatureFromWmi();
    }

    private static float ReadCpuTemperatureFromWmi()
    {
        float result = 0f;

        WmiHelper.TryExecute("ThermalZone", () =>
        {
            var scope = new ManagementScope(@"root\WMI");
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"));

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                if (obj["CurrentTemperature"] is not ushort raw)
                    continue;

                var celsius = (raw - 2732) / 10f;
                if (celsius is > 0f and < 150f)
                {
                    result = celsius;
                    return true;
                }
            }

            return true;
        });

        return result;
    }
}

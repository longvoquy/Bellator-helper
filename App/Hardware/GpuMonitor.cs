using LibreHardwareMonitor.Hardware;

namespace BHelper.App.Hardware;

public sealed class GpuMonitor : PollingMonitorBase
{
    private readonly object _sync = new();

    public GpuMonitor(int intervalMs = 2000)
        : base(intervalMs)
    {
    }

    public float Load { get; private set; }
    public float Temperature { get; private set; }
    public float MemoryUsedMb { get; private set; }
    public float MemoryTotalMb { get; private set; }
    public float ClockMhz { get; private set; }

    protected override void Refresh()
    {
        var computer = HardwareMonitorHost.Computer;
        if (computer is null)
            return;

        var gpu = FindNvidiaGpu(computer);
        if (gpu is null)
        {
            lock (_sync)
            {
                Load = 0f;
                Temperature = 0f;
                MemoryUsedMb = 0f;
                MemoryTotalMb = 0f;
                ClockMhz = 0f;
            }
            return;
        }

        var load = ReadSensor(gpu, SensorType.Load) ?? 0f;
        var temp = ReadSensor(gpu, SensorType.Temperature) ?? 0f;
        var clock = ReadSensor(gpu, SensorType.Clock, "Core")
                    ?? ReadSensor(gpu, SensorType.Clock)
                    ?? 0f;

        var memoryUsed = ReadSensor(gpu, SensorType.SmallData, "Memory Used")
                         ?? ReadSensor(gpu, SensorType.Data, "Memory Used")
                         ?? 0f;
        var memoryTotal = ReadSensor(gpu, SensorType.SmallData, "Memory Total")
                          ?? ReadSensor(gpu, SensorType.Data, "Memory Total")
                          ?? 0f;

        lock (_sync)
        {
            Load = load;
            Temperature = temp;
            MemoryUsedMb = memoryUsed;
            MemoryTotalMb = memoryTotal;
            ClockMhz = clock;
        }
    }

    private static IHardware? FindNvidiaGpu(Computer computer)
    {
        var nvidia = SensorReader.FindHardware(computer, HardwareType.GpuNvidia);
        if (nvidia is not null)
            return nvidia;

        return computer.Hardware.FirstOrDefault(h =>
            h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);
    }

    private static float? ReadSensor(IHardware hardware, SensorType type, string? nameContains = null)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != type || !sensor.Value.HasValue)
                continue;

            if (nameContains is null ||
                sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                return sensor.Value;
        }

        foreach (var sub in hardware.SubHardware)
        {
            var value = ReadSensor(sub, type, nameContains);
            if (value.HasValue)
                return value;
        }

        return null;
    }
}

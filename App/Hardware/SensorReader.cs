using LibreHardwareMonitor.Hardware;

namespace LecooHelper.App.Hardware;

internal static class SensorReader
{
    public static float? FirstValue(
        Computer computer,
        HardwareType hardwareType,
        SensorType sensorType,
        string? nameContains = null)
    {
        foreach (var hardware in EnumerateHardware(computer))
        {
            if (hardware.HardwareType != hardwareType)
                continue;

            var value = FirstOnHardware(hardware, sensorType, nameContains);
            if (value.HasValue)
                return value;
        }

        return null;
    }

    public static float? MaxValue(
        Computer computer,
        HardwareType hardwareType,
        SensorType sensorType,
        string? nameContains = null)
    {
        float? max = null;

        foreach (var hardware in EnumerateHardware(computer))
        {
            if (hardware.HardwareType != hardwareType)
                continue;

            foreach (var value in AllOnHardware(hardware, sensorType, nameContains))
                max = max.HasValue ? Math.Max(max.Value, value) : value;
        }

        return max;
    }

    public static float? BestCpuTemperature(Computer computer)
    {
        float? package = null;
        float? maxCore = null;
        float? any = null;

        foreach (var hardware in computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
                continue;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue)
                    continue;

                var value = sensor.Value.Value;
                if (!IsPlausibleTemperature(value))
                    continue;

                var name = sensor.Name;
                if (IsIgnoredCpuTempName(name))
                    continue;

                if (IsPackageTempName(name))
                {
                    package = package.HasValue ? Math.Max(package.Value, value) : value;
                    continue;
                }

                if (IsCoreTempName(name))
                {
                    maxCore = maxCore.HasValue ? Math.Max(maxCore.Value, value) : value;
                    continue;
                }

                any = any.HasValue ? Math.Max(any.Value, value) : value;
            }
        }

        if (package.HasValue)
            return package;

        if (maxCore.HasValue)
            return maxCore;

        if (any.HasValue)
            return any;

        return BestCpuTemperatureFromMotherboard(computer);
    }

    private static float? BestCpuTemperatureFromMotherboard(Computer computer)
    {
        float? best = null;

        foreach (var hardware in EnumerateHardware(computer))
        {
            if (hardware.HardwareType != HardwareType.Motherboard)
                continue;

            foreach (var sensor in EnumerateSensors(hardware))
            {
                if (sensor.SensorType != SensorType.Temperature || !sensor.Value.HasValue)
                    continue;

                var value = sensor.Value.Value;
                if (!IsPlausibleTemperature(value))
                    continue;

                var name = sensor.Name;
                if (IsIgnoredCpuTempName(name))
                    continue;

                if (!name.Contains("CPU", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    continue;

                best = best.HasValue ? Math.Max(best.Value, value) : value;
            }
        }

        return best;
    }

    private static bool IsPackageTempName(string name) =>
        name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Core Max", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase);

    private static bool IsCoreTempName(string name) =>
        name.Contains("Core", StringComparison.OrdinalIgnoreCase);

    private static bool IsIgnoredCpuTempName(string name) =>
        name.Contains("Distance", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("TjMax", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Tj Max", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<float> AllValues(Computer computer, SensorType sensorType)
    {
        var values = new List<float>();

        foreach (var hardware in EnumerateHardware(computer))
        {
            foreach (var sensor in EnumerateSensors(hardware))
            {
                if (sensor.SensorType == sensorType && sensor.Value.HasValue)
                    values.Add(sensor.Value.Value);
            }
        }

        return values;
    }

    public static IHardware? FindHardware(Computer computer, HardwareType hardwareType)
    {
        foreach (var hardware in EnumerateHardware(computer))
        {
            if (hardware.HardwareType == hardwareType)
                return hardware;
        }

        return null;
    }

    private static IEnumerable<IHardware> EnumerateHardware(Computer computer)
    {
        foreach (var hardware in computer.Hardware)
        {
            yield return hardware;
            foreach (var sub in EnumerateSubHardware(hardware))
                yield return sub;
        }
    }

    private static IEnumerable<IHardware> EnumerateSubHardware(IHardware hardware)
    {
        foreach (var sub in hardware.SubHardware)
        {
            yield return sub;
            foreach (var nested in EnumerateSubHardware(sub))
                yield return nested;
        }
    }

    private static IEnumerable<ISensor> EnumerateSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
            yield return sensor;

        foreach (var sub in hardware.SubHardware)
        {
            foreach (var sensor in EnumerateSensors(sub))
                yield return sensor;
        }
    }

    private static bool IsPlausibleTemperature(float celsius) =>
        celsius is >= 0f and < 150f;

    private static float? FirstOnHardware(IHardware hardware, SensorType sensorType, string? nameContains)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != sensorType || !sensor.Value.HasValue)
                continue;

            if (nameContains is null ||
                sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                return sensor.Value;
        }

        foreach (var sub in hardware.SubHardware)
        {
            var value = FirstOnHardware(sub, sensorType, nameContains);
            if (value.HasValue)
                return value;
        }

        return null;
    }

    private static IEnumerable<float> AllOnHardware(IHardware hardware, SensorType sensorType, string? nameContains)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != sensorType || !sensor.Value.HasValue)
                continue;

            if (nameContains is null ||
                sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                yield return sensor.Value.Value;
        }

        foreach (var sub in hardware.SubHardware)
        {
            foreach (var value in AllOnHardware(sub, sensorType, nameContains))
                yield return value;
        }
    }
}

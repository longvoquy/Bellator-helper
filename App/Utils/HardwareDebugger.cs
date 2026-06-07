using System.Management;
using System.Text;
using LibreHardwareMonitor.Hardware;
using BHelper.App.Hardware;
using Microsoft.Win32;

namespace BHelper.App.Utils;

public static class HardwareDebugger
{
    private static readonly string[] ServiceNameFilters = ["lecoo", "bellator", "fighter", "n176"];

    public static string DumpAll()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        var path = Path.Combine(logDir, "hardware_dump.txt");

        var sb = new StringBuilder();
        sb.AppendLine($"{AppBranding.ShortName} hardware dump");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Exe: {AppContext.BaseDirectory}");
        sb.AppendLine($"Admin: {AdminHelper.IsRunningAsAdministrator()}");
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            sb.AppendLine("NOTE: CPU temperature sensors stay null without Administrator.");
            sb.AppendLine($"      Run {AppBranding.ShortName}.exe as Administrator (same as LibreHardwareMonitor).");
        }
        sb.AppendLine();

        try
        {
            DumpLibreHardwareMonitor(sb);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"[LHM ERROR] {ex}");
        }

        sb.AppendLine();
        DumpWmi(sb);

        sb.AppendLine();
        DumpRegistryServices(sb);

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static void DumpLibreHardwareMonitor(StringBuilder sb)
    {
        sb.AppendLine("=== LibreHardwareMonitor ===");

        HardwareMonitorHost.Acquire();
        try
        {
            for (var i = 0; i < 3; i++)
            {
                HardwareMonitorHost.UpdateAll();
                Thread.Sleep(500);
            }

            var computer = HardwareMonitorHost.Computer;
            if (computer is null)
            {
                sb.AppendLine("Computer is null.");
                return;
            }

            sb.AppendLine($"BestCpuTemperature: {SensorReader.BestCpuTemperature(computer)}");
            sb.AppendLine();

            foreach (var hardware in computer.Hardware)
                DumpHardware(sb, hardware, 0);

            sb.AppendLine();
            sb.AppendLine("--- CPU temperature sensors only ---");
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                    continue;

                sb.AppendLine($"CPU node: {hardware.Name} ({hardware.HardwareType})");
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature)
                        continue;

                    sb.AppendLine($"  [{sensor.Name}] Value={FormatValue(sensor.Value)} Min={FormatValue(sensor.Min)} Max={FormatValue(sensor.Max)}");
                }
            }
        }
        finally
        {
            HardwareMonitorHost.Release();
        }
    }

    private static void DumpHardware(StringBuilder sb, IHardware hardware, int depth)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}{hardware.Name} | Type={hardware.HardwareType} | Sensors={hardware.Sensors.Length}");

        foreach (var sensor in hardware.Sensors)
        {
            sb.AppendLine(
                $"{indent}  {sensor.SensorType,-12} {sensor.Name,-30} Value={FormatValue(sensor.Value)}");
        }

        foreach (var sub in hardware.SubHardware)
            DumpHardware(sb, sub, depth + 1);
    }

    private static string FormatValue(float? value) =>
        value.HasValue ? value.Value.ToString("0.###") : "null";

    private static void DumpWmi(StringBuilder sb)
    {
        sb.AppendLine("=== WMI (selected) ===");

        DumpWmiQuery(sb, "Win32_Processor", "SELECT Name, CurrentClockSpeed FROM Win32_Processor");
        DumpWmiQuery(sb, "Win32_Fan", "SELECT Name, DesiredSpeed FROM Win32_Fan");
        DumpWmiQuery(sb, "MSAcpi_ThermalZone", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature", @"root\WMI");
    }

    private static void DumpWmiQuery(StringBuilder sb, string title, string query, string? scopePath = null)
    {
        sb.AppendLine($"--- {title} ---");
        try
        {
            ManagementObjectSearcher searcher;
            if (scopePath is null)
                searcher = new ManagementObjectSearcher(query);
            else
            {
                var scope = new ManagementScope(scopePath);
                scope.Connect();
                searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            }

            var count = 0;
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                count++;
                foreach (var prop in obj.Properties)
                {
                    if (prop.Value is null)
                        continue;
                    sb.AppendLine($"  {prop.Name} = {prop.Value}");
                }
                sb.AppendLine();
            }

            if (count == 0)
                sb.AppendLine("  (no rows)");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  ERROR: {ex.Message}");
        }
    }

    private static void DumpRegistryServices(StringBuilder sb)
    {
        sb.AppendLine("=== Registry Services (filtered) ===");
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (key is null)
            {
                sb.AppendLine("Cannot open Services key.");
                return;
            }

            foreach (var name in key.GetSubKeyNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                if (!ServiceNameFilters.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    continue;

                sb.AppendLine(name);
                try
                {
                    using var sub = key.OpenSubKey(name);
                    var imagePath = sub?.GetValue("ImagePath") as string;
                    if (!string.IsNullOrWhiteSpace(imagePath))
                        sb.AppendLine($"  ImagePath: {imagePath}");
                }
                catch
                {
                    // skip
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR: {ex.Message}");
        }
    }
}

using LibreHardwareMonitor.Hardware;

namespace BHelper.App.Hardware;

internal static class HardwareMonitorHost
{
    private static readonly object Sync = new();
    private static Computer? _computer;
    private static int _refCount;

    public static void Acquire()
    {
        lock (Sync)
        {
            if (_computer is null)
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsPsuEnabled = false,
                };
                _computer.Open();
            }

            _refCount++;
        }
    }

    public static void Release()
    {
        lock (Sync)
        {
            if (_refCount <= 0)
                return;

            _refCount--;
            if (_refCount > 0)
                return;

            _computer?.Close();
            _computer = null;
        }
    }

    public static Computer? Computer
    {
        get
        {
            lock (Sync)
                return _computer;
        }
    }

    public static void UpdateAll()
    {
        lock (Sync)
        {
            if (_computer is null)
                return;

            foreach (var hardware in _computer.Hardware)
                UpdateRecursive(hardware);
        }
    }

    private static void UpdateRecursive(IHardware hardware)
    {
        foreach (var sub in hardware.SubHardware)
            UpdateRecursive(sub);

        hardware.Update();
    }
}

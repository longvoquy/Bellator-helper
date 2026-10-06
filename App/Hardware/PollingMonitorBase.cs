namespace BHelper.App.Hardware;

public abstract class PollingMonitorBase : IDisposable
{
    private System.Timers.Timer? _timer;
    private bool _started;

    protected PollingMonitorBase(int intervalMs = 2000)
    {
        IntervalMs = intervalMs;
    }

    public int IntervalMs { get; }

    // Monitors that read only WMI (battery, RAM) return false so they never wait for
    // LibreHardwareMonitor to open, which takes seconds at startup.
    protected virtual bool UsesHardwareHost => true;

    public event EventHandler? Updated;

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        if (UsesHardwareHost)
            HardwareMonitorHost.Acquire();

        _timer = new System.Timers.Timer(IntervalMs);
        _timer.Elapsed += (_, _) => OnTimerElapsed();
        _timer.AutoReset = true;
        _timer.Start();

        OnTimerElapsed();
    }

    public void Stop()
    {
        if (!_started)
            return;

        _started = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        if (UsesHardwareHost)
            HardwareMonitorHost.Release();
    }

    protected abstract void Refresh();

    protected void RaiseUpdated() => Updated?.Invoke(this, EventArgs.Empty);

    public void PollNow()
    {
        try
        {
            if (UsesHardwareHost)
                HardwareMonitorHost.UpdateAll();
            Refresh();
        }
        catch
        {
            // Ignore transient sensor/WMI errors.
        }
    }

    private void OnTimerElapsed()
    {
        try
        {
            if (UsesHardwareHost)
                HardwareMonitorHost.UpdateAll();
            Refresh();
            RaiseUpdated();
        }
        catch
        {
            // Ignore transient sensor/WMI errors between polls.
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}

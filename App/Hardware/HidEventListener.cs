using System.Management;

namespace BHelper.App.Hardware;

// Listens to HID_EVENT20 WMI events (Bellator Fn keys). Fn+F10 arrives as EventDetail 01-23-xx
// (type HotKey, name OpenApp). Optionally logs every raw event for diagnosis.
public sealed class HidEventListener : IDisposable
{
    private const byte HotKeyType = 1;
    private const byte OpenAppName = 0x23;
    private const byte SystemPerModeName = 0x0F;
    private const int DebounceMs = 300;
    private const int DuplicateWindowMs = 100;

    private readonly string? _logPath;
    private readonly object _sync = new();
    private ManagementEventWatcher? _watcher;
    private long _lastPressMs;
    private long _lastModeEventMs;
    private int _lastModeValue = -1;

    public HidEventListener(string? logPath = null)
    {
        _logPath = logPath;
        if (logPath is not null)
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    }

    // Raised on a WMI thread when the Bellator key (Fn+F10) is pressed.
    public event EventHandler? BellatorKeyPressed;

    // Raised on a WMI thread when the firmware reports a performance mode (raw value 0..3).
    public event EventHandler<byte>? SystemPerModeChanged;

    public bool Start()
    {
        try
        {
            var scope = new ManagementScope(@"root\WMI");
            _watcher = new ManagementEventWatcher(scope, new EventQuery("SELECT * FROM HID_EVENT20"));
            _watcher.EventArrived += OnEventArrived;
            _watcher.Start();
            Write("listening for HID_EVENT20");
            return true;
        }
        catch (Exception ex)
        {
            Write($"start failed: {ex.Message}");
            return false;
        }
    }

    private void OnEventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var detail = e.NewEvent["EventDetail"] as byte[];
            if (detail is null || detail.Length < 2)
            {
                Write("event without EventDetail");
                return;
            }

            Write($"EventDetail {BitConverter.ToString(detail)}");

            if (detail[0] != HotKeyType)
                return;

            if (detail[1] == SystemPerModeName && detail.Length > 2)
            {
                RaiseModeChanged(detail[2]);
                return;
            }

            if (detail[1] != OpenAppName)
                return;

            var now = Environment.TickCount64;
            if (now - Interlocked.Read(ref _lastPressMs) < DebounceMs)
                return;

            Interlocked.Exchange(ref _lastPressMs, now);
            BellatorKeyPressed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Write($"read failed: {ex.Message}");
        }
    }

    // The firmware sends each mode event twice, about 1 ms apart. Drop the repeat.
    private void RaiseModeChanged(byte value)
    {
        var now = Environment.TickCount64;
        if (value == _lastModeValue && now - Interlocked.Read(ref _lastModeEventMs) < DuplicateWindowMs)
            return;

        _lastModeValue = value;
        Interlocked.Exchange(ref _lastModeEventMs, now);
        SystemPerModeChanged?.Invoke(this, value);
    }

    private void Write(string message)
    {
        if (_logPath is null)
            return;

        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            lock (_sync)
            {
                File.AppendAllText(_logPath, line);
            }
        }
        catch
        {
            // Logging must never break the app.
        }
    }

    public void Dispose()
    {
        if (_watcher is null)
            return;

        _watcher.EventArrived -= OnEventArrived;
        _watcher.Stop();
        _watcher.Dispose();
        _watcher = null;
    }
}

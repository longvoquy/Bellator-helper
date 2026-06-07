using BHelper.App.Hardware;
using BHelper.App.Power;
using BHelper.App.Utils;

namespace BHelper.App.Tray;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsForm _settingsForm;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Settings _settings;
    private readonly CpuMonitor _cpuMonitor;
    private readonly GpuMonitor _gpuMonitor;
    private ToolStripMenuItem _cpuMenuItem = null!;
    private ToolStripMenuItem _gpuMenuItem = null!;
    private readonly List<ToolStripMenuItem> _powerModeItems = [];
    private readonly SynchronizationContext _syncContext;
    private Icon? _currentTrayIcon;
    private bool _disposed;
    private long _lastTrayHoverRefreshMs;

    public TrayApp()
    {
        _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = Settings.Load();
        var interval = _settings.UpdateIntervalMs;

        _cpuMonitor = new CpuMonitor(interval);
        _gpuMonitor = new GpuMonitor(interval);

        if (!PowerMode.TrySyncFromSystem())
            PowerMode.SetMode(_settings.DefaultPowerMode);

        _settingsForm = new SettingsForm();
        _settingsForm.ModeChangeRequested += OnSettingsFormModeChangeRequested;

        _currentTrayIcon = AppIconHelper.CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _currentTrayIcon,
            Text = AppBranding.ShortName,
            Visible = true
        };

        _contextMenu = BuildContextMenu();
        // Do not assign ContextMenuStrip to NotifyIcon — it blocks left-click on Windows 11.
        _notifyIcon.MouseUp += OnTrayIconMouseUp;
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
        _notifyIcon.MouseMove += OnTrayIconMouseMove;

        _cpuMonitor.Updated += OnMonitorUpdated;
        _gpuMonitor.Updated += OnMonitorUpdated;

        _cpuMonitor.Start();
        _gpuMonitor.Start();

        Application.ApplicationExit += (_, _) => DisposeTray();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip
        {
            ShowCheckMargin = true
        };

        _cpuMenuItem = CreateInfoItem("CPU Temp: --");
        _gpuMenuItem = CreateInfoItem("GPU Temp: --");

        menu.Items.Add(_cpuMenuItem);
        menu.Items.Add(_gpuMenuItem);
        menu.Items.Add(new ToolStripSeparator());

        var modeTitle = CreateInfoItem("Mode");
        menu.Items.Add(modeTitle);

        foreach (var profile in PerformanceProfile.All)
        {
            var item = new ToolStripMenuItem(profile.Name)
            {
                Tag = profile.Kind,
                CheckOnClick = false
            };
            item.Click += OnPowerModeItemClick;
            _powerModeItems.Add(item);
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        var openItem = new ToolStripMenuItem("Open Dashboard");
        openItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(openItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem("Exit");
        quitItem.Click += (_, _) => ExitThread();
        menu.Items.Add(quitItem);

        return menu;
    }

    private void OnPowerModeItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: PowerModeKind kind })
            return;

        ApplyPowerMode(kind);
    }

    private void OnSettingsFormModeChangeRequested(object? sender, PowerModeKind kind) =>
        ApplyPowerMode(kind);

    private void ApplyPowerMode(PowerModeKind kind)
    {
        if (!PowerMode.SetMode(kind))
            return;

        _settings.DefaultPowerMode = kind;
        _settings.Save();
        UpdatePowerModeChecks();
        PostRefreshTray();
    }

    private void UpdatePowerModeChecks()
    {
        foreach (var item in _powerModeItems)
        {
            if (item.Tag is PowerModeKind kind)
                item.Checked = kind == PowerMode.Current;
        }
    }

    private void OnMonitorUpdated(object? sender, EventArgs e) => PostRefreshTray();

    private void PostRefreshTray()
    {
        _syncContext.Post(_ => RefreshTrayDisplay(), null);
    }

    private void RefreshTrayDisplay()
    {
        if (_disposed)
            return;

        PowerMode.TrySyncFromSystem();

        var snapshot = CreateSnapshot();

        _cpuMenuItem.Text = $"CPU Temp: {FormatCpuTemp(snapshot.CpuTemp)}";
        _gpuMenuItem.Text = $"GPU Temp: {FormatTemp(snapshot.GpuTemp)}";

        _notifyIcon.Text = TruncateTooltip(
            $"CPU {FormatCpuTemp(snapshot.CpuTemp)} | GPU {FormatTemp(snapshot.GpuTemp)}");

        UpdatePowerModeChecks();

        if (_settingsForm.Visible)
            _settingsForm.ApplySnapshot(snapshot, PowerMode.Current);
    }

    private void RefreshSensorsNow()
    {
        _cpuMonitor.PollNow();
        _gpuMonitor.PollNow();
        RefreshTrayDisplay();
    }

    private HardwareSnapshot CreateSnapshot() =>
        new(_cpuMonitor.Temp, _gpuMonitor.Temperature);

    private void ShowSettings()
    {
        if (_disposed || _settingsForm.IsDisposed)
            return;

        _syncContext.Post(_ =>
        {
            if (_disposed || _settingsForm.IsDisposed)
                return;

            try
            {
                _settingsForm.PositionBottomRight();
                RefreshSensorsNow();
                _settingsForm.ApplySnapshot(CreateSnapshot(), PowerMode.Current);
                _settingsForm.ShowAll();
            }
            catch
            {
                // Keep tray alive if sensor read fails during open.
                _settingsForm.ShowAll();
            }
        }, null);
    }

    private void OnTrayIconMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            ShowSettings();
        else if (e.Button == MouseButtons.Right)
            _contextMenu.Show(Cursor.Position);
    }

    private void OnTrayIconMouseMove(object? sender, MouseEventArgs e)
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (now - _lastTrayHoverRefreshMs < 500)
            return;

        _lastTrayHoverRefreshMs = now;
        PostRefreshTray();
    }

    private static ToolStripMenuItem CreateInfoItem(string text) =>
        new(text) { Enabled = false };

    private static string FormatTemp(float celsius) =>
        celsius > 0f ? $"{celsius:0}°C" : "--";

    private static string FormatCpuTemp(float celsius)
    {
        if (celsius > 0f)
            return $"{celsius:0}°C";

        return AdminHelper.IsRunningAsAdministrator()
            ? "--"
            : "N/A (run as admin)";
    }

    private static string TruncateTooltip(string text) =>
        text.Length <= 63 ? text : text[..60] + "...";

    private void DisposeTray()
    {
        if (_disposed)
            return;
        _disposed = true;

        _cpuMonitor.Dispose();
        _gpuMonitor.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _settingsForm.Dispose();
        AppIconHelper.DisposeIcon(_currentTrayIcon);
        _currentTrayIcon = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DisposeTray();
        base.Dispose(disposing);
    }
}

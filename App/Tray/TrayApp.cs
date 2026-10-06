using System.Diagnostics;
using BHelper.App.Hardware;
using BHelper.App.Power;
using BHelper.App.Utils;
using Microsoft.Win32;

namespace BHelper.App.Tray;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly GlobalHotkeyWindow _hotkeyWindow;
    private SettingsForm? _settingsForm;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Settings _settings;
    private readonly CpuMonitor _cpuMonitor;
    private readonly GpuMonitor _gpuMonitor;
    private readonly FanMonitor _fanMonitor;
    private readonly RamMonitor _ramMonitor;
    private readonly BatteryMonitor _batteryMonitor;
    private readonly HidEventListener _hidEventListener;
    private ToolStripMenuItem _cpuMenuItem = null!;
    private ToolStripMenuItem _gpuMenuItem = null!;
    private ToolStripMenuItem _batteryMenuItem = null!;
    private readonly List<ToolStripMenuItem> _powerModeItems = [];
    private readonly SynchronizationContext _syncContext;
    private Icon? _currentTrayIcon;
    private bool _disposed;
    private long _lastTrayHoverRefreshMs;
    private long _lastPowerSyncMs;
    private int _powerSyncRunning;
    private readonly System.Windows.Forms.Timer _powerDebounceTimer;

    private const int PowerSyncIntervalMs = 5000;
    private const int PowerDebounceMs = 3000;

    public TrayApp()
    {
        _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = Settings.Load();
        var interval = _settings.UpdateIntervalMs;
        if (_settings.EnablePowerDebugLog)
            PowerDebugLog.Enable(Settings.DataDirectory);

        _cpuMonitor = new CpuMonitor(interval);
        _gpuMonitor = new GpuMonitor(interval);
        _fanMonitor = new FanMonitor(interval);
        _ramMonitor = new RamMonitor(interval);
        _batteryMonitor = new BatteryMonitor();
        _hidEventListener = new HidEventListener(
            _settings.EnableHidEventLog ? Path.Combine(Settings.DataDirectory, "logs", "hid_events.txt") : null);
        // The firmware announces every mode change (Fn+F10, Control Center, our own SetMode): refresh at once.
        _hidEventListener.SystemPerModeChanged += (_, raw) =>
        {
            if (PowerMode.TryApplyReportedMode(raw))
                PostRefreshTray();
        };
        // Fn+F10 (Bellator key): cycle the performance mode, same as the keyboard hotkey.
        _hidEventListener.BellatorKeyPressed += (_, _) =>
            ApplyPowerModeInBackground(PowerMode.Next(PowerMode.Current), persist: true, notify: true);

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

        _hotkeyWindow = new GlobalHotkeyWindow();
        RegisterModeHotkey();

        // Plugging/unplugging can fire several status events in a row; apply only the last state.
        _powerDebounceTimer = new System.Windows.Forms.Timer { Interval = PowerDebounceMs };
        _powerDebounceTimer.Tick += (_, _) =>
        {
            _powerDebounceTimer.Stop();
            ApplyAutoPowerMode();
        };
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _cpuMonitor.Updated += OnMonitorUpdated;
        _gpuMonitor.Updated += OnMonitorUpdated;
        _fanMonitor.Updated += OnMonitorUpdated;
        _ramMonitor.Updated += OnMonitorUpdated;
        _batteryMonitor.Updated += OnMonitorUpdated;

        Application.ApplicationExit += (_, _) => DisposeTray();

        // LibreHardwareMonitor open + WMI calls are slow; keep them off the UI thread
        // so the tray icon appears and responds immediately.
        Task.Run(() =>
        {
            if (!PowerMode.TrySyncFromSystem())
                PowerMode.SetMode(_settings.DefaultPowerMode);

            // Apply the power-source mode right away instead of waiting for the next plug/unplug event.
            var startupTarget = ModeForCurrentPowerSource();
            if (startupTarget != PowerMode.Current)
                PowerMode.SetMode(startupTarget);

            _lastPowerSyncMs = Environment.TickCount64;
            PostRefreshTray();

            _cpuMonitor.Start();
            _gpuMonitor.Start();
            _fanMonitor.Start();
            _ramMonitor.Start();
            _batteryMonitor.Start();
            _hidEventListener.Start();
        });
    }

    private SettingsForm GetSettingsForm()
    {
        if (_settingsForm is null)
        {
            _settingsForm = new SettingsForm(_settings);
            _settingsForm.ModeChangeRequested += OnSettingsFormModeChangeRequested;
            _settingsForm.HotkeyChangeRequested += OnHotkeyChangeRequested;
        }

        return _settingsForm;
    }

    private void QueuePowerSync()
    {
        var now = Environment.TickCount64;
        if (now - Interlocked.Read(ref _lastPowerSyncMs) < PowerSyncIntervalMs)
            return;

        if (Interlocked.Exchange(ref _powerSyncRunning, 1) == 1)
            return;

        Interlocked.Exchange(ref _lastPowerSyncMs, now);
        Task.Run(() =>
        {
            try
            {
                var before = PowerMode.Current;
                if (PowerMode.TrySyncFromSystem() && PowerMode.Current != before)
                    PostRefreshTray();
            }
            finally
            {
                Interlocked.Exchange(ref _powerSyncRunning, 0);
            }
        });
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip
        {
            ShowCheckMargin = true
        };

        _cpuMenuItem = CreateInfoItem("CPU Temp: --");
        _gpuMenuItem = CreateInfoItem("GPU Temp: --");
        _batteryMenuItem = CreateInfoItem("Battery: --");

        menu.Items.Add(_cpuMenuItem);
        menu.Items.Add(_gpuMenuItem);
        menu.Items.Add(_batteryMenuItem);
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

        var updateItem = new ToolStripMenuItem("Check for updates");
        updateItem.Click += OnCheckUpdatesClick;
        menu.Items.Add(updateItem);

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

        CommitPowerMode(kind, persist: true);
    }

    // WMI calls are slow; run them off the UI thread and commit the result on the UI thread.
    private void ApplyPowerModeInBackground(PowerModeKind kind, bool persist, bool notify = false)
    {
        Task.Run(() =>
        {
            if (!PowerMode.SetMode(kind))
                return;

            _syncContext.Post(_ =>
            {
                CommitPowerMode(kind, persist);
                if (notify)
                    ShowNotice($"Mode: {PerformanceProfile.GetDisplayName(kind)}");
            }, null);
        });
    }

    private void ApplyAutoPowerMode()
    {
        var target = ModeForCurrentPowerSource();
        if (target == PowerMode.Current)
            return;

        ApplyPowerModeInBackground(target, persist: false, notify: true);
    }

    private PowerModeKind ModeForCurrentPowerSource() =>
        PowerMode.IsOnAcPower() ? _settings.AcPowerMode : _settings.BatteryPowerMode;

    private void ShowNotice(string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(2000, AppBranding.FullName, text, icon);
    }

    private void OnHotkeyChangeRequested(object? sender, SettingsForm.HotkeyRequest request)
    {
        var previousModifiers = _settings.HotkeyModifiers;
        var previousKey = _settings.HotkeyKey;

        if (_hotkeyWindow.Register(request.Modifiers, request.Key))
        {
            _settings.HotkeyModifiers = request.Modifiers;
            _settings.HotkeyKey = request.Key;
            _settings.Save();
            ShowNotice("Mode hotkey updated.");
        }
        else
        {
            _hotkeyWindow.Register(previousModifiers, previousKey);
            ShowNotice("That hotkey is unavailable. It may be used by another app.", ToolTipIcon.Warning);
        }

        _settingsForm?.RefreshSettingsView();
    }

    private void CommitPowerMode(PowerModeKind kind, bool persist)
    {
        Interlocked.Exchange(ref _lastPowerSyncMs, Environment.TickCount64);
        if (persist)
        {
            // A mode the user picks is remembered for the current power source, so it comes back next time.
            _settings.DefaultPowerMode = kind;
            if (PowerMode.IsOnAcPower())
                _settings.AcPowerMode = kind;
            else
                _settings.BatteryPowerMode = kind;

            _settings.Save();
        }

        UpdatePowerModeChecks();
        PostRefreshTray();
    }

    private void RegisterModeHotkey()
    {
        _hotkeyWindow.Pressed += (_, _) =>
            ApplyPowerModeInBackground(PowerMode.Next(PowerMode.Current), persist: true, notify: true);

        if (!_hotkeyWindow.Register(_settings.HotkeyModifiers, _settings.HotkeyKey))
        {
            _notifyIcon.ShowBalloonTip(
                3000,
                AppBranding.FullName,
                "Could not register the mode hotkey. It may be used by another app.",
                ToolTipIcon.Warning);
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.StatusChange)
            return;

        // Restart the debounce timer on the UI thread; the mode is applied once the power state settles.
        _syncContext.Post(_ =>
        {
            _powerDebounceTimer.Stop();
            _powerDebounceTimer.Start();
        }, null);
    }


    private async void OnCheckUpdatesClick(object? sender, EventArgs e)
    {
        var latest = await UpdateChecker.GetLatestVersionAsync();
        if (_disposed)
            return;

        var current = UpdateChecker.CurrentVersion;
        if (latest is null)
        {
            MessageBox.Show("Could not check for updates.", AppBranding.FullName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (latest.CompareTo(current) <= 0)
        {
            MessageBox.Show($"You are up to date ({current}).", AppBranding.FullName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(
            $"Version {latest} is available (current {current}). Open the download page?",
            AppBranding.FullName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
            Process.Start(new ProcessStartInfo(UpdateChecker.LatestReleaseUrl) { UseShellExecute = true });
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

        QueuePowerSync();

        var snapshot = CreateSnapshot();

        _cpuMenuItem.Text = $"CPU Temp: {FormatCpuTemp(snapshot.CpuTemp)}";
        _gpuMenuItem.Text = $"GPU Temp: {FormatTemp(snapshot.GpuTemp)}";
        _batteryMenuItem.Text = FormatBattery(_batteryMonitor);

        _notifyIcon.Text = TruncateTooltip(
            $"CPU {FormatCpuTemp(snapshot.CpuTemp)} | GPU {FormatTemp(snapshot.GpuTemp)}");

        UpdatePowerModeChecks();

        if (_settingsForm is { Visible: true })
            _settingsForm.ApplySnapshot(snapshot, PowerMode.Current);
    }

    private HardwareSnapshot CreateSnapshot() =>
        new(_cpuMonitor.Temp, _gpuMonitor.Temperature, _fanMonitor.CpuRpm, _fanMonitor.GpuRpm,
            _ramMonitor.UsagePercent, _ramMonitor.UsedGb, _ramMonitor.TotalGb);

    private void ShowSettings()
    {
        if (_disposed)
            return;

        _syncContext.Post(_ =>
        {
            if (_disposed)
                return;

            var form = GetSettingsForm();
            if (form.IsDisposed)
                return;

            try
            {
                // Use cached monitor values (refreshed by timers) so the window opens instantly.
                form.PositionBottomRight();
                form.ApplySnapshot(CreateSnapshot(), PowerMode.Current);
            }
            catch
            {
                // Keep tray alive if layout/snapshot fails during open.
            }

            form.ShowAll();
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

    private static string FormatBattery(BatteryMonitor battery)
    {
        if (!battery.HasBattery)
            return "Battery: none";

        var health = battery.HealthPercent > 0 ? $"{battery.HealthPercent}%" : "--";
        var source = battery.OnAcPower ? "AC" : "battery";
        return $"Battery: {battery.ChargePercent}% ({source}) | Health {health} | {battery.CycleCount} cycles";
    }

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
        _fanMonitor.Dispose();
        _ramMonitor.Dispose();
        _batteryMonitor.Dispose();
        _powerDebounceTimer.Dispose();
        _hidEventListener.Dispose();

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _hotkeyWindow.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _settingsForm?.Dispose();
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

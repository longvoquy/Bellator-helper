using LecooHelper.App.Power;
using LecooHelper.App.Utils;

namespace LecooHelper.App.Tray;

public sealed class SettingsForm : Form
{
    private const int FormW = 340;
    private const int FormH = 270;
    private const int FormPad = 12;
    private const int CornerRadius = 10;
    private const int InnerW = FormW - FormPad * 2;
    private const int HeaderHeight = 44;

    private readonly Label _cpuTempLabel;
    private readonly Label _gpuTempLabel;
    private readonly Label _modeLabel;
    private readonly ProgressBar _cpuBar;
    private readonly ProgressBar _gpuBar;
    private readonly Button _btnSilent;
    private readonly Button _btnBalanced;
    private readonly Button _btnPerformance;

    private bool _dragging;
    private Point _dragStart;

    public event EventHandler<PowerModeKind>? ModeChangeRequested;

    public SettingsForm()
    {
        ClientSize = new Size(FormW, FormH);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = TrayTheme.Background;
        ForeColor = TrayTheme.Text;
        Padding = new Padding(0);
        Region = RoundedRegion(FormW, FormH, CornerRadius);

        var header = BuildHeader();

        _cpuTempLabel = MakeValueLabel("--");
        _gpuTempLabel = MakeValueLabel("--");
        _modeLabel = MakeValueLabel("--", 11f);

        _cpuBar = MakeBar();
        _gpuBar = MakeBar();

        var cpuRow = BuildStatRow("CPU", _cpuTempLabel, _cpuBar);
        var gpuRow = BuildStatRow("GPU", _gpuTempLabel, _gpuBar);
        var modeRow = BuildModeRow(out _btnSilent, out _btnBalanced, out _btnPerformance);

        _btnSilent.Click += (_, _) => ModeChangeRequested?.Invoke(this, PowerModeKind.Silent);
        _btnBalanced.Click += (_, _) => ModeChangeRequested?.Invoke(this, PowerModeKind.Balanced);
        _btnPerformance.Click += (_, _) => ModeChangeRequested?.Invoke(this, PowerModeKind.Performance);

        var mainPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = TrayTheme.Background,
            Padding = new Padding(FormPad, 6, FormPad, FormPad)
        };
        mainPanel.Controls.Add(cpuRow);
        mainPanel.Controls.Add(gpuRow);
        mainPanel.Controls.Add(modeRow);

        Controls.Add(mainPanel);
        Controls.Add(header);

        header.MouseDown += OnDragStart;
        header.MouseMove += OnDragMove;
        header.MouseUp += OnDragEnd;
        MouseDown += OnDragStart;
        MouseMove += OnDragMove;
        MouseUp += OnDragEnd;

        FormClosing += OnFormClosing;
    }

    public void PositionBottomRight()
    {
        var screen = Screen.FromPoint(Cursor.Position)
                     ?? Screen.PrimaryScreen
                     ?? Screen.AllScreens[0];
        var area = screen.WorkingArea;
        Left = area.Right - Width - FormPad;
        Top = area.Bottom - Height - FormPad;
    }

    public void ShowAll()
    {
        if (IsDisposed) return;
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
        Focus();
        TopMost = true;
        TopMost = false;
    }

    public void HideAll() => Hide();

    internal void ApplySnapshot(HardwareSnapshot snapshot, string modeName)
    {
        _cpuTempLabel.Text = FormatCpuTemp(snapshot.CpuTemp);
        _cpuTempLabel.ForeColor = TrayTheme.TempAccent(snapshot.CpuTemp);
        _cpuBar.Value = TempPercent(snapshot.CpuTemp);
        _cpuBar.ForeColor = TrayTheme.TempAccent(snapshot.CpuTemp);

        _gpuTempLabel.Text = FormatTemp(snapshot.GpuTemp);
        _gpuTempLabel.ForeColor = TrayTheme.TempAccent(snapshot.GpuTemp);
        _gpuBar.Value = TempPercent(snapshot.GpuTemp);
        _gpuBar.ForeColor = TrayTheme.TempAccent(snapshot.GpuTemp);

        _modeLabel.Text = modeName;
        HighlightModeButton(modeName);
    }

    private Panel BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight,
            BackColor = TrayTheme.Background,
            Padding = new Padding(14, 0, 6, 0)
        };

        var title = new Label
        {
            Text = "Lecoo Helper",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };

        var closeBtn = new Button
        {
            Text = "X",
            Dock = DockStyle.Right,
            Width = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TrayTheme.TextMuted,
            BackColor = TrayTheme.Background,
            TabStop = false,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10f)
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = TrayTheme.HeaderHover;
        closeBtn.Click += (_, _) => HideAll();

        header.Paint += (_, e) =>
        {
            using var pen = new Pen(TrayTheme.Border, 1);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        header.Controls.Add(title);
        header.Controls.Add(closeBtn);
        return header;
    }

    private static Panel BuildStatRow(string title, Label valueLabel, ProgressBar bar)
    {
        var panel = new Panel
        {
            Width = InnerW,
            Height = 58,
            BackColor = TrayTheme.Background,
            Padding = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = title,
            Location = new Point(0, 4),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TrayTheme.TextMuted,
            BackColor = TrayTheme.Background
        };

        valueLabel.Location = new Point(60, 0);

        bar.Location = new Point(0, 34);
        bar.Width = InnerW;
        bar.Height = 4;

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(bar);
        return panel;
    }

    private Panel BuildModeRow(out Button silent, out Button balanced, out Button performance)
    {
        var panel = new Panel
        {
            Width = InnerW,
            Height = 60,
            BackColor = TrayTheme.Background,
            Padding = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = "Mode",
            Location = new Point(0, 4),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TrayTheme.TextMuted,
            BackColor = TrayTheme.Background
        };

        const int btnW = 100, gap = 8, startY = 26;

        silent = MakeModeButton("Silent", new Point(0, startY));
        balanced = MakeModeButton("Balanced", new Point(btnW + gap, startY));
        performance = MakeModeButton("Performance", new Point((btnW + gap) * 2, startY));

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(silent);
        panel.Controls.Add(balanced);
        panel.Controls.Add(performance);
        return panel;
    }

    private static Label MakeValueLabel(string text, float size = 18f) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", size, FontStyle.Bold),
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };

    private static ProgressBar MakeBar() =>
        new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 4,
            Style = ProgressBarStyle.Continuous,
            ForeColor = TrayTheme.TempAccent(50f),
            BackColor = TrayTheme.GaugeTrack
        };

    private static Button MakeModeButton(string text, Point location) =>
        new()
        {
            Text = text,
            Location = location,
            Size = new Size(100, 30),
            FlatStyle = FlatStyle.Flat,
            ForeColor = TrayTheme.TextMuted,
            BackColor = TrayTheme.Surface,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            TabStop = false
        };

    private void OnDragStart(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragStart = e.Location;
    }

    private void OnDragMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Left += e.X - _dragStart.X;
        Top += e.Y - _dragStart.Y;
    }

    private void OnDragEnd(object? sender, MouseEventArgs e) => _dragging = false;

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        HideAll();
    }

    private void HighlightModeButton(string modeName)
    {
        foreach (var btn in new[] { _btnSilent, _btnBalanced, _btnPerformance })
        {
            var active = btn.Text.Equals(modeName, StringComparison.OrdinalIgnoreCase);
            btn.ForeColor = active ? TrayTheme.GaugeFill : TrayTheme.TextMuted;
            btn.FlatAppearance.BorderColor = active ? TrayTheme.GaugeFill : TrayTheme.Border;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = active ? TrayTheme.ModeActiveFill : TrayTheme.Surface;
        }
    }

    private static int TempPercent(float celsius) =>
        celsius > 0f ? Math.Clamp((int)celsius, 0, 100) : 0;

    private static string FormatTemp(float celsius) =>
        celsius > 0f ? $"{celsius:0}°C" : "--";

    private static string FormatCpuTemp(float celsius)
    {
        if (celsius > 0f) return $"{celsius:0}°C";
        return AdminHelper.IsRunningAsAdministrator() ? "--" : "N/A (run as admin)";
    }

    private static Region RoundedRegion(int w, int h, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
        path.AddArc(w - radius * 2, 0, radius * 2, radius * 2, 270, 90);
        path.AddArc(w - radius * 2, h - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(0, h - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }
}

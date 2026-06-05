using LecooHelper.App.Power;
using LecooHelper.App.Utils;

namespace LecooHelper.App.Tray;

public sealed class SettingsForm : Form
{
    private const int FormW = 420;
    private const int FormH = 300;
    private const int FormPad = 12;
    private const int CornerRadius = 10;
    private const int InnerW = FormW - FormPad * 2;
    private const int HeaderHeight = 44;
    private const int StatRowHeight = 36;

    private readonly Label _cpuTempLabel;
    private readonly Label _gpuTempLabel;
    private readonly List<Button> _modeButtons = [];

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

        _cpuTempLabel = MakeStatValueLabel("--");
        _gpuTempLabel = MakeStatValueLabel("--");

        var modeRow = BuildModeRow();
        var cpuRow = BuildStatRow("CPU", _cpuTempLabel);
        var gpuRow = BuildStatRow("GPU", _gpuTempLabel);

        var mainPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = TrayTheme.Background,
            Padding = new Padding(FormPad, 6, FormPad, FormPad)
        };
        mainPanel.Controls.Add(modeRow);
        mainPanel.Controls.Add(cpuRow);
        mainPanel.Controls.Add(gpuRow);

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

    internal void ApplySnapshot(HardwareSnapshot snapshot, PowerModeKind mode)
    {
        _cpuTempLabel.Text = FormatCpuTemp(snapshot.CpuTemp);
        _cpuTempLabel.ForeColor = TrayTheme.Text;

        _gpuTempLabel.Text = FormatTemp(snapshot.GpuTemp);
        _gpuTempLabel.ForeColor = TrayTheme.Text;

        HighlightModeButton(mode);
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
            Font = TrayTheme.Title,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };

        var closeBtn = new Button
        {
            Text = "X",
            Dock = DockStyle.Right,
            Width = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background,
            TabStop = false,
            Cursor = Cursors.Hand,
            Font = TrayTheme.CloseButton
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = TrayTheme.Background;
        closeBtn.FlatAppearance.MouseDownBackColor = TrayTheme.Background;
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

    private static Panel BuildStatRow(string title, Label valueLabel)
    {
        var panel = new Panel
        {
            Width = InnerW,
            Height = StatRowHeight,
            BackColor = TrayTheme.Background
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(TrayTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        // TableLayoutPanel chia 2 cột: trái AutoSize, phải Fill
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = TrayTheme.Background,
            Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // cột trái: vừa đủ chứa "CPU"/"GPU"
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // cột phải: lấy hết phần còn lại

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = TrayTheme.SectionLabel,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        valueLabel.Padding = new Padding(0, 0, 10, 0);

        table.Controls.Add(titleLabel, 0, 0);
        table.Controls.Add(valueLabel, 1, 0);

        panel.Controls.Add(table);
        return panel;
    }

    private Panel BuildModeRow()
    {
        var profiles = PerformanceProfile.All;

        const int gap = 6;
        const int btnH = 32;
        const int modeLabelY = 2;
        const int modeLabelRowH = 18;
        const int modeLabelButtonGap = 10;
        const int modeButtonY = modeLabelY + modeLabelRowH + modeLabelButtonGap;
        const int panelBottomPad = 8;
        var btnW = (InnerW - gap * (profiles.Count - 1)) / profiles.Count;

        var panel = new Panel
        {
            Width = InnerW,
            Height = modeButtonY + btnH + panelBottomPad,
            BackColor = TrayTheme.Background,
            Padding = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = "Mode",
            Location = new Point(0, modeLabelY),
            AutoSize = true,
            Font = TrayTheme.SectionLabel,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };
        panel.Controls.Add(titleLabel);

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            var btn = new Button
            {
                Text = profile.Name,
                Location = new Point(i * (btnW + gap), modeButtonY),
                Size = new Size(btnW, btnH),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TrayTheme.Text,
                BackColor = TrayTheme.Surface,
                Font = TrayTheme.Body,
                Cursor = Cursors.Hand,
                TabStop = false,
                Tag = profile.Kind
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = TrayTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = TrayTheme.Surface;
            btn.FlatAppearance.MouseDownBackColor = TrayTheme.Surface;

            btn.Click += (_, _) => ModeChangeRequested?.Invoke(this, profile.Kind);

            _modeButtons.Add(btn);
            panel.Controls.Add(btn);
        }

        return panel;
    }

    private static Label MakeStatValueLabel(string text) =>
        new()
        {
            Text = text,
            AutoSize = false,
            Font = TrayTheme.StatValue,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
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

    private void HighlightModeButton(PowerModeKind mode)
    {
        foreach (var btn in _modeButtons)
        {
            var active = btn.Tag is PowerModeKind kind && kind == mode;
            if (active)
            {
                var fill = TrayTheme.ModeFill(mode);
                btn.ForeColor = TrayTheme.ModeForeColor(mode);
                btn.BackColor = fill;
                btn.FlatAppearance.BorderColor = TrayTheme.Border;
                btn.FlatAppearance.MouseOverBackColor = fill;
                btn.FlatAppearance.MouseDownBackColor = fill;
            }
            else
            {
                btn.ForeColor = TrayTheme.Text;
                btn.BackColor = TrayTheme.Surface;
                btn.FlatAppearance.BorderColor = TrayTheme.Border;
                btn.FlatAppearance.MouseOverBackColor = TrayTheme.Surface;
                btn.FlatAppearance.MouseDownBackColor = TrayTheme.Surface;
            }
        }
    }

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

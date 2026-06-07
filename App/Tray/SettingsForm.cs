using System.Drawing.Drawing2D;
using BHelper.App.Power;
using BHelper.App.Utils;

namespace BHelper.App.Tray;

public sealed class SettingsForm : Form
{
    private const int FormW = 324;
    private const int FormH = 344;
    private const int FormPad = 12;
    private const int CornerRadius = 10;
    private const int InnerW = FormW - FormPad * 2;
    private const int HeaderHeight = 36;
    private const int StatRowHeight = 32;
    private const int SectionGap = 6;

    // G-Helper tablePerf: 4 equal columns, single row, Margin(4) per button.
    private const int ModeIconDisplaySize = 28;
    private const int ModeTableRowHeight = 72;
    private const int ModeButtonMargin = 4;
    private const int ModeButtonBorderRadius = 5;
    private const int ModeLabelY = 2;
    private const int ModeLabelRowH = 18;
    private const int ModeLabelButtonGap = 6;
    private const int ModePanelBottomPad = 6;

    private readonly Label _cpuTempLabel;
    private readonly Label _gpuTempLabel;
    private readonly List<ModeButton> _modeButtons = [];

    private bool _dragging;
    private Point _dragStart;

    public event EventHandler<PowerModeKind>? ModeChangeRequested;

    public SettingsForm()
    {
        ClientSize = new Size(FormW, FormH);
        Icon = AppIconHelper.CreateTrayIcon();
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

        var startupCheckUpdating = false;
        var startupCheck = new CheckBox
        {
            Text = "Run at startup",
            Checked = StartupHelper.IsEnabled(),
            Font = TrayTheme.Body,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background,
            Padding = new Padding(10, 0, 0, 0),
            AutoSize = true
        };
        startupCheck.CheckedChanged += (_, _) =>
        {
            if (startupCheckUpdating)
                return;

            var ok = startupCheck.Checked ? StartupHelper.Enable() : StartupHelper.Disable();
            if (ok)
                return;

            startupCheckUpdating = true;
            startupCheck.Checked = !startupCheck.Checked;
            startupCheckUpdating = false;

            MessageBox.Show(
                "Could not update startup settings.",
                AppBranding.FullName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        mainPanel.Controls.Add(modeRow);
        mainPanel.Controls.Add(cpuRow);
        mainPanel.Controls.Add(gpuRow);
        mainPanel.Controls.Add(startupCheck);

        Controls.Add(mainPanel);
        Controls.Add(header);

        header.MouseDown += OnDragStart;
        header.MouseMove += OnDragMove;
        header.MouseUp += OnDragEnd;
        MouseDown += OnDragStart;
        MouseMove += OnDragMove;
        MouseUp += OnDragEnd;

        FormClosing += OnFormClosing;

        HighlightModeButton(PowerMode.Current);
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
            Text = AppBranding.ShortName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = TrayTheme.Title,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };

        var closeBtn = new Button
        {
            Image = ResourceImageHelper.Load("close_23.png"),
            Dock = DockStyle.Right,
            Width = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background,
            TabStop = false,
            Cursor = Cursors.Hand,
            ImageAlign = ContentAlignment.MiddleCenter,
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
            Margin = new Padding(0, 0, 0, SectionGap),
            BackColor = TrayTheme.Background
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(TrayTheme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = TrayTheme.Background,
            Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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
        var titleHeight = ModeLabelY + ModeLabelRowH;
        var panelHeight = titleHeight + ModeLabelButtonGap + ModeTableRowHeight + ModePanelBottomPad;

        var panel = new Panel
        {
            Width = InnerW,
            Height = panelHeight,
            Margin = new Padding(0, 0, 0, SectionGap),
            BackColor = TrayTheme.Background
        };

        var titleLabel = new Label
        {
            Text = "Mode",
            Location = new Point(0, ModeLabelY),
            AutoSize = true,
            Font = TrayTheme.SectionLabel,
            ForeColor = TrayTheme.Text,
            BackColor = TrayTheme.Background
        };
        panel.Controls.Add(titleLabel);

        var table = new TableLayoutPanel
        {
            ColumnCount = profiles.Count,
            RowCount = 1,
            Location = new Point(0, titleHeight + ModeLabelButtonGap),
            Size = new Size(InnerW, ModeTableRowHeight),
            BackColor = TrayTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        for (var i = 0; i < profiles.Count; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / profiles.Count));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeTableRowHeight));

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            var btn = new ModeButton(profile.Kind, profile.Name, LoadModeIcon(profile.Kind));
            btn.Click += (_, _) =>
            {
                HighlightModeButton(profile.Kind);
                ModeChangeRequested?.Invoke(this, profile.Kind);
            };

            _modeButtons.Add(btn);
            table.Controls.Add(btn, i, 0);
        }

        panel.Controls.Add(table);
        return panel;
    }

    private static Image? LoadModeIcon(PowerModeKind kind)
    {
        var file = kind switch
        {
            PowerModeKind.Silent => "energy_savings_48.png",
            PowerModeKind.Balanced => "infinite_48.png",
            PowerModeKind.Beast => "rocket_launch_48.png",
            PowerModeKind.Battle => "stadia_controller_48.png",
            _ => null
        };
        if (file is null)
            return null;

        using var original = ResourceImageHelper.Load(file);
        return original is null ? null : ScaleModeIcon(original, ModeIconDisplaySize);
    }

    private static Image ScaleModeIcon(Image source, int size)
    {
        var scaled = new Bitmap(size, size);
        using var g = Graphics.FromImage(scaled);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawImage(source, 0, 0, size, size);
        return scaled;
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
            btn.Selected = btn.Kind == mode;
    }

    private static string FormatTemp(float celsius) =>
        celsius > 0f ? $"{celsius:0}°C" : "--";

    private static string FormatCpuTemp(float celsius)
    {
        if (celsius > 0f) return $"{celsius:0}°C";
        return AdminHelper.IsRunningAsAdministrator() ? "--" : "N/A (run as admin)";
    }

    private sealed class ModeButton : Button
    {
        private const int SelectedBorderWidth = 2;

        private bool _selected;
        private readonly Color _borderColor;

        public PowerModeKind Kind { get; }

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                    return;
                _selected = value;
                Invalidate();
            }
        }

        public ModeButton(PowerModeKind kind, string label, Image? icon)
        {
            Kind = kind;
            _borderColor = TrayTheme.ModeFill(kind);

            Text = label;
            Image = icon;
            TextImageRelation = TextImageRelation.ImageAboveText;
            ImageAlign = ContentAlignment.BottomCenter;

            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = TrayTheme.Surface;
            ForeColor = TrayTheme.ModeForeColor(kind);
            Font = TrayTheme.Body;
            Cursor = Cursors.Hand;
            TabStop = false;
            Margin = new Padding(ModeButtonMargin);
            Dock = DockStyle.Fill;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            var rect = ClientRectangle;
            var border = SelectedBorderWidth;
            var radius = ModeButtonBorderRadius;
            var borderDrawColor = _selected ? _borderColor : Color.Transparent;
            var surfaceColor = Parent?.BackColor ?? TrayTheme.Background;

            using var pathSurface = GetRoundedPath(rect, radius + border);
            using var pathBorder = GetRoundedPath(Rectangle.Inflate(rect, -border, -border), radius);
            using var penSurface = new Pen(surfaceColor, border);
            using var penBorder = new Pen(borderDrawColor, border) { Alignment = PenAlignment.Outset };

            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Region = new Region(pathSurface);
            pevent.Graphics.DrawPath(penSurface, pathSurface);
            pevent.Graphics.DrawPath(penBorder, pathBorder);
        }

        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var curve = radius * 2f;
            var arc = new RectangleF(rect.X, rect.Y, curve, curve);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - curve;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - curve;
            path.AddArc(arc, 0, 90);
            arc.X = rect.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
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

using BHelper.App.Power;
using BHelper.App.Utils;

namespace BHelper.App.Tray;

// Separate small window for the manual fan cap. Sizes are logical (96 DPI) and scaled with DeviceDpi.
internal sealed class FanForm : Form
{
    private const int FormWidth = 380;
    private const int Pad = 20;

    private readonly Panel _header = new();
    private readonly Label _title = new();
    private readonly Button _closeButton = new();
    private readonly Label _modeLabel = new();
    private readonly CheckBox _limitCheck = new();
    private readonly FanBlock _cpuGpu = new("CPU / GPU fan");
    private readonly FanBlock _sys = new("System fan");
    private readonly Label _note = new();
    private readonly Button _applyButton = new();
    private readonly Button _autoButton = new();
    private readonly FlowLayoutPanel _body = new();

    private PowerModeKind _mode = PowerModeKind.Balanced;
    private bool _binding;
    private bool _dragging;
    private Point _dragStart;

    public sealed record FanApplyRequest(bool Enabled, int CpuGpuValue, int SysValue);

    public event EventHandler<FanApplyRequest>? ApplyRequested;

    public FanForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = TrayTheme.Background;
        ForeColor = TrayTheme.Text;
        Font = TrayTheme.Body;
        Icon = AppIconHelper.CreateTrayIcon();

        BuildLayout();
        WireEvents();
        Paint += (_, e) =>
        {
            using var pen = new Pen(TrayTheme.ModeButtonBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    private int S(int logical) => (int)Math.Round(logical * DeviceDpi / 96.0);

    private void BuildLayout()
    {
        SuspendLayout();

        // Header: title, drag area and close button.
        _header.Dock = DockStyle.Top;
        _header.Height = S(44);
        _header.BackColor = TrayTheme.Background;
        _header.Padding = new Padding(S(Pad), 0, S(8), 0);

        _title.Text = "Fan control";
        _title.Font = TrayTheme.Title;
        _title.ForeColor = TrayTheme.Text;
        _title.Dock = DockStyle.Fill;
        _title.TextAlign = ContentAlignment.MiddleLeft;

        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = S(40);
        _closeButton.FlatStyle = FlatStyle.Flat;
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.FlatAppearance.MouseOverBackColor = TrayTheme.Surface;
        _closeButton.FlatAppearance.MouseDownBackColor = TrayTheme.Surface;
        _closeButton.BackColor = TrayTheme.Background;
        _closeButton.Cursor = Cursors.Hand;
        _closeButton.TabStop = false;
        _closeButton.Image = ResourceImageHelper.LoadTinted("cross-23.png", TrayTheme.Text);
        _closeButton.ImageAlign = ContentAlignment.MiddleCenter;

        _header.Controls.Add(_title);
        _header.Controls.Add(_closeButton);

        // Body: stacked blocks.
        _body.Dock = DockStyle.Top;
        _body.FlowDirection = FlowDirection.TopDown;
        _body.WrapContents = false;
        _body.AutoSize = true;
        _body.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _body.Padding = new Padding(S(Pad), S(4), S(Pad), S(Pad));
        _body.BackColor = TrayTheme.Background;

        var contentWidth = S(FormWidth - Pad * 2);

        _modeLabel.AutoSize = false;
        _modeLabel.Width = contentWidth;
        _modeLabel.Height = S(22);
        _modeLabel.ForeColor = TrayTheme.TooltipTitle;
        _modeLabel.Margin = new Padding(0, 0, 0, S(6));

        _limitCheck.Text = "Limit max fan speed";
        _limitCheck.AutoSize = false;
        _limitCheck.Width = contentWidth;
        _limitCheck.Height = S(28);
        _limitCheck.ForeColor = TrayTheme.Text;
        _limitCheck.BackColor = TrayTheme.Background;
        _limitCheck.Margin = new Padding(0, 0, 0, S(10));

        _cpuGpu.Setup(contentWidth, this);
        _sys.Setup(contentWidth, this);

        _note.AutoSize = false;
        _note.Width = contentWidth;
        _note.Height = S(76);
        _note.ForeColor = TrayTheme.TooltipTitle;
        _note.Text = "The limit is a ceiling, not a fixed speed: the fan runs at its normal speed and is only held back "
                     + "when it wants to go faster (heavy load). Off means Auto. A lower limit is quieter but hotter. "
                     + "Each mode has its own allowed range; a quieter mode allows a lower ceiling.";
        _note.Margin = new Padding(0, S(2), 0, S(10));

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = TrayTheme.Background
        };
        StyleButton(_applyButton, "Apply");
        StyleButton(_autoButton, "Reset to auto");
        buttons.Controls.Add(_applyButton);
        buttons.Controls.Add(_autoButton);

        _body.Controls.Add(_modeLabel);
        _body.Controls.Add(_limitCheck);
        _body.Controls.Add(_cpuGpu.Panel);
        _body.Controls.Add(_sys.Panel);
        _body.Controls.Add(_note);
        _body.Controls.Add(buttons);

        Controls.Add(_body);
        Controls.Add(_header);

        ClientSize = new Size(S(FormWidth), S(400));
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(S(FormWidth), 0);
        ResumeLayout(true);
    }

    private void StyleButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Size = new Size(S(120), S(32));
        button.Margin = new Padding(0, 0, S(10), 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = TrayTheme.ModeButtonBorder;
        button.FlatAppearance.MouseOverBackColor = TrayTheme.Surface;
        button.BackColor = TrayTheme.Background;
        button.ForeColor = TrayTheme.Text;
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
    }

    private void WireEvents()
    {
        _closeButton.Click += (_, _) => Hide();

        // Keep the window alive so the next open is instant, like the dashboard.
        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            Hide();
        };

        foreach (var control in new Control[] { _header, _title })
        {
            control.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _dragging = true;
                _dragStart = e.Location;
            };
            control.MouseMove += (_, e) =>
            {
                if (!_dragging) return;
                Left += e.X - _dragStart.X;
                Top += e.Y - _dragStart.Y;
            };
            control.MouseUp += (_, _) => _dragging = false;
        }

        _limitCheck.CheckedChanged += (_, _) =>
        {
            if (!_binding)
                UpdateEnabledState();
        };

        _cpuGpu.Slider.ValueChanged += (_, _) => _cpuGpu.RefreshValueText(_mode, FanKind.CpuGpu);
        _sys.Slider.ValueChanged += (_, _) => _sys.RefreshValueText(_mode, FanKind.Sys);

        _applyButton.Click += (_, _) =>
            ApplyRequested?.Invoke(this, new FanApplyRequest(_limitCheck.Checked, _cpuGpu.Slider.Value, _sys.Slider.Value));

        _autoButton.Click += (_, _) =>
        {
            _limitCheck.Checked = false;
            ApplyRequested?.Invoke(this, new FanApplyRequest(false, _cpuGpu.Slider.Value, _sys.Slider.Value));
        };
    }

    // Sets ranges for the mode and shows the saved values.
    public void Bind(PowerModeKind mode, bool enabled, int cpuGpuValue, int sysValue)
    {
        _binding = true;
        _mode = mode;
        _modeLabel.Text = $"Mode: {PerformanceProfile.GetDisplayName(mode)}";

        var cpuRange = FanControl.GetRange(mode, FanKind.CpuGpu);
        var sysRange = FanControl.GetRange(mode, FanKind.Sys);
        _cpuGpu.Slider.SetRange(cpuRange.Min, cpuRange.Max, cpuGpuValue);
        _sys.Slider.SetRange(sysRange.Min, sysRange.Max, sysValue);
        _cpuGpu.RefreshValueText(mode, FanKind.CpuGpu);
        _sys.RefreshValueText(mode, FanKind.Sys);

        _limitCheck.Checked = enabled;
        UpdateEnabledState();
        _binding = false;
    }

    public PowerModeKind BoundMode => _mode;

    internal void UpdateLive(HardwareSnapshot snapshot)
    {
        _cpuGpu.Now.Text = $"Running: {snapshot.CpuFanRpm} RPM";
        _sys.Now.Text = $"Running: {snapshot.SysFanRpm} RPM";
    }

    private void UpdateEnabledState()
    {
        _cpuGpu.Slider.Enabled = _limitCheck.Checked;
        _sys.Slider.Enabled = _limitCheck.Checked;
    }

    // One fan: title with the chosen cap, a slider, then the live RPM and the allowed range.
    private sealed class FanBlock
    {
        private readonly string _name;
        private readonly Label _caption = new();
        private readonly Label _value = new();
        private readonly Label _rangeMin = new();
        private readonly Label _rangeMax = new();

        public FanBlock(string name) => _name = name;

        public Panel Panel { get; } = new();
        public DarkSlider Slider { get; } = new();
        public Label Now { get; } = new();

        public void Setup(int width, FanForm owner)
        {
            var rowHeight = owner.S(22);

            Panel.Width = width;
            Panel.Height = owner.S(110);
            Panel.Margin = new Padding(0, 0, 0, owner.S(8));
            Panel.BackColor = TrayTheme.Background;

            _caption.Text = _name;
            _caption.Font = TrayTheme.SectionLabel;
            _caption.ForeColor = TrayTheme.Text;
            _caption.SetBounds(0, 0, width / 2, rowHeight);

            _value.TextAlign = ContentAlignment.MiddleRight;
            _value.ForeColor = TrayTheme.Text;
            _value.SetBounds(width / 2, 0, width - width / 2, rowHeight);

            Slider.SetBounds(0, rowHeight + owner.S(2), width, owner.S(28));

            // The allowed range sits under the two ends of the slider, so the slider is its own scale.
            var scaleTop = rowHeight + owner.S(30);
            _rangeMin.TextAlign = ContentAlignment.MiddleLeft;
            _rangeMin.ForeColor = TrayTheme.TooltipTitle;
            _rangeMin.SetBounds(0, scaleTop, width / 2, rowHeight);

            _rangeMax.TextAlign = ContentAlignment.MiddleRight;
            _rangeMax.ForeColor = TrayTheme.TooltipTitle;
            _rangeMax.SetBounds(width / 2, scaleTop, width - width / 2, rowHeight);

            Now.ForeColor = TrayTheme.Text;
            Now.Text = "Running: -- RPM";
            Now.SetBounds(0, scaleTop + rowHeight + owner.S(4), width, rowHeight);

            foreach (var label in new[] { _caption, _value, Now, _rangeMin, _rangeMax })
            {
                label.AutoSize = false;
                label.BackColor = TrayTheme.Background;
            }

            Panel.Controls.AddRange([_caption, _value, Slider, _rangeMin, _rangeMax, Now]);
        }

        public void RefreshValueText(PowerModeKind mode, FanKind fan)
        {
            _value.Text = $"Max {FanControl.ToRpm(Slider.Value)} RPM";

            var range = FanControl.GetRange(mode, fan);
            _rangeMin.Text = $"{FanControl.ToRpm(range.Min)}";
            _rangeMax.Text = $"{FanControl.ToRpm(range.Max)}";
        }
    }
}

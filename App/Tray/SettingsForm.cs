using System.Drawing.Drawing2D;
using BHelper.App.Power;
using BHelper.App.Utils;

namespace BHelper.App.Tray;

public sealed class SettingsForm : Form
{
    private const int DesignDpi = 192;
    private const int FormW = 848;
    private const int FormH = 580;
    private const int CornerRadius = 20;

    private const int FormPad = 22;
    private const int FormPadTop = 16;
    private const int FormPadBottom = 24;
    private const int InnerW = FormW - FormPad * 2;
    private const int HeaderHeight = 72;

    private const int StatRowHeight = 52;
    private const int StatLabelWidth = 112;
    private const int StatLabelPadLeft = 0;
    private const int StatValuePadRight = 20;

    private const int ModeTableRowHeight = 140;
    private const int ModeButtonMargin = 8;
    private const int ModeButtonBorderRadius = 10;
    private const int ModeSectionIconSize = 32;
    private const int ModeLabelH = 32;
    private const int ModeLabelButtonGap = 8;
    private const int ModePanelBottomPad = 0;
    private const int StartupRowHeight = 48;

    private readonly Panel headerPanel;
    private readonly Label titleLabel;
    private readonly Button closeButton;
    private readonly Panel mainPanel;
    private readonly TableLayoutPanel contentLayout;
    private readonly Panel modePanel;
    private readonly TableLayoutPanel modeLayout;
    private readonly TableLayoutPanel modeTitleTable;
    private readonly PictureBox modeIconBox;
    private readonly Label modeTextLabel;
    private readonly Panel modeButtonsTable;
    private readonly Panel gpuPanel;
    private readonly TableLayoutPanel gpuTable;
    private readonly Label gpuTitleLabel;
    private readonly Label cpuTempLabel;
    private readonly Label gpuTempLabel;
    private readonly Panel ramPanel;
    private readonly TableLayoutPanel ramTable;
    private readonly Label ramTitleLabel;
    private readonly Label ramValueLabel;
    private readonly CheckBox startupCheckBox;
    private readonly FlowLayoutPanel hotkeyPanel;
    private readonly Label hotkeyLabel;
    private readonly Button hotkeyButton;
    private readonly Settings _settings;
    private readonly List<ModeButton> _modeButtons = [];

    private bool _dragging;
    private bool _startupCheckUpdating;
    private bool _capturingHotkey;
    private Point _dragStart;


    public sealed record HotkeyRequest(int Modifiers, int Key);

    public event EventHandler<PowerModeKind>? ModeChangeRequested;
    public event EventHandler<HotkeyRequest>? HotkeyChangeRequested;

    private int ScaleDpi(int value) => (int)Math.Round(value * DeviceDpi / (double)DesignDpi);

    public SettingsForm(Settings settings)
    {
        _settings = settings;
        headerPanel = new Panel();
        titleLabel = new Label();
        closeButton = new Button();
        mainPanel = new Panel();
        contentLayout = new TableLayoutPanel();
        modePanel = new Panel();
        modeLayout = new TableLayoutPanel();
        modeTitleTable = new TableLayoutPanel();
        modeIconBox = new PictureBox();
        modeTextLabel = new Label();
        modeButtonsTable = new Panel();
        gpuPanel = new Panel();
        gpuTable = new TableLayoutPanel();
        gpuTitleLabel = new Label();
        cpuTempLabel = new Label();
        gpuTempLabel = new Label();
        ramPanel = new Panel();
        ramTable = new TableLayoutPanel();
        ramTitleLabel = new Label();
        ramValueLabel = new Label();
        startupCheckBox = new CheckBox();
        hotkeyPanel = new FlowLayoutPanel();
        hotkeyLabel = new Label();
        hotkeyButton = new Button();

        BuildHotkeySection();
        InitializeComponent();
        ConfigureResponsiveLayout();

        Icon = AppIconHelper.CreateTrayIcon();
        Region?.Dispose();
        Region = RoundedRegion(FormW, FormH, CornerRadius);

        titleLabel.Text = AppBranding.ShortName;
        closeButton.Image = ResourceImageHelper.Load("cross-23.png");
        using var modeSectionIcon = ResourceImageHelper.Load("readiness_score_32dp_fill.png");
        modeIconBox.Image = modeSectionIcon is null ? null : ScaleModeIcon(modeSectionIcon, ScaleDpi(ModeSectionIconSize));

        ApplyTheme();
        BuildModeButtons();
        ConfigureStartupCheck();
        WireEvents();
        RefreshSettingsView();
        HighlightModeButton(PowerMode.Current);
    }

    public void PositionBottomRight()
    {
        PerformLayout();
        var screen = Screen.FromPoint(Cursor.Position)
                     ?? Screen.PrimaryScreen
                     ?? Screen.AllScreens[0];
        var area = screen.WorkingArea;
        Left = area.Right - Width - ScaleDpi(FormPad);
        Top = area.Bottom - Height - ScaleDpi(FormPad);
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
        cpuTempLabel.Text = $"CPU: {FormatCpuTemp(snapshot.CpuTemp)}  Fan: {FormatRpm(snapshot.CpuFanRpm)}";
        cpuTempLabel.ForeColor = TrayTheme.Text;

        gpuTempLabel.Text = $"{FormatTemp(snapshot.GpuTemp)}  {FormatRpm(snapshot.GpuFanRpm)}";
        gpuTempLabel.ForeColor = TrayTheme.Text;

        ramValueLabel.Text = FormatRam(snapshot);
        ramValueLabel.ForeColor = TrayTheme.Text;

        HighlightModeButton(mode);
    }

    private static string FormatRpm(int rpm) =>
        rpm > 0 ? $"{rpm} RPM" : "0 RPM";

    private static string FormatRam(HardwareSnapshot snapshot) =>
        snapshot.RamTotalGb > 0f
            ? $"{snapshot.RamUsagePercent:0}%  ({snapshot.RamUsedGb:0.0} / {snapshot.RamTotalGb:0} GB)"
            : "--";

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

        Region?.Dispose();
        Region = RoundedRegion(ClientSize.Width, ClientSize.Height, ScaleDpi(CornerRadius));
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);

        ConfigureResponsiveLayout();
        BuildModeButtons();
        HighlightModeButton(PowerMode.Current);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        ClientSize = new Size(FormW, FormH);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        ForeColor = Color.White;
        AutoScaleDimensions = new SizeF(192F, 192F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(820, 140);

        // headerPanel
        headerPanel.SuspendLayout();
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Padding = new Padding(20, 0, 12, 0);
        headerPanel.Size = new Size(FormW, HeaderHeight);

        // titleLabel
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Location = new Point(20, 0);
        titleLabel.Margin = new Padding(0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(FormW - 20 - 84, HeaderHeight);
        titleLabel.Text = "B-helper";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        // closeButton
        closeButton.Cursor = Cursors.Hand;
        closeButton.Dock = DockStyle.Right;
        closeButton.FlatStyle = FlatStyle.Flat;
        closeButton.Location = new Point(FormW - 84, 0);
        closeButton.Margin = new Padding(0);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(72, HeaderHeight);
        closeButton.TabStop = false;
        closeButton.UseVisualStyleBackColor = false;

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(closeButton);
        headerPanel.ResumeLayout(false);

        // mainPanel
        mainPanel.SuspendLayout();
        mainPanel.Dock = DockStyle.Top;
        mainPanel.Location = new Point(0, HeaderHeight);
        mainPanel.Margin = new Padding(0);
        mainPanel.Name = "mainPanel";
        mainPanel.Padding = new Padding(0, FormPadTop, 0, FormPadBottom);
        mainPanel.Size = new Size(FormW, FormH - HeaderHeight);
        mainPanel.AutoSize = true;
        mainPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // contentLayout
        contentLayout.SuspendLayout();
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Dock = DockStyle.Top;
        contentLayout.Location = new Point(0, 0);
        contentLayout.Margin = Padding.Empty;
        contentLayout.Name = "contentLayout";
        contentLayout.RowCount = 5;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentLayout.Size = new Size(FormW, FormH - HeaderHeight - FormPadTop - FormPadBottom);
        contentLayout.AutoSize = true;
        contentLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // modePanel
        modePanel.SuspendLayout();
        modePanel.Dock = DockStyle.Top;
        modePanel.Location = new Point(0, 0);
        modePanel.Margin = Padding.Empty;
        modePanel.Padding = new Padding(20, 0, 20, 0);
        modePanel.Name = "modePanel";
        modePanel.Size = new Size(FormW, ModeLabelH + ModeLabelButtonGap + ModeTableRowHeight + ModePanelBottomPad);
        modePanel.AutoSize = true;
        modePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // modeLayout
        modeLayout.SuspendLayout();
        modeLayout.ColumnCount = 1;
        modeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        modeLayout.Dock = DockStyle.Top;
        modeLayout.Location = new Point(20, 0);
        modeLayout.Margin = new Padding(0);
        modeLayout.Name = "modeLayout";
        modeLayout.RowCount = 2;
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeLabelH + ModeLabelButtonGap));
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeTableRowHeight));
        modeLayout.Size = new Size(InnerW, ModeLabelH + ModeLabelButtonGap + ModeTableRowHeight + ModePanelBottomPad);
        modeLayout.AutoSize = true;
        modeLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // modeTitleTable
        modeTitleTable.SuspendLayout();
        modeTitleTable.ColumnCount = 3;
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ModeSectionIconSize));
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        modeTitleTable.RowCount = 1;
        modeTitleTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        modeTitleTable.Dock = DockStyle.Fill;
        modeTitleTable.Location = new Point(0, 0);
        modeTitleTable.Margin = Padding.Empty;
        modeTitleTable.Name = "modeTitleTable";
        modeTitleTable.Size = new Size(InnerW, ModeLabelH + ModeLabelButtonGap);
        modeTitleTable.AutoSize = true;
        modeTitleTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        // modeIconBox
        modeIconBox.Dock = DockStyle.Fill;
        modeIconBox.Margin = Padding.Empty;
        modeIconBox.Name = "modeIconBox";
        modeIconBox.Size = new Size(ModeSectionIconSize, ModeSectionIconSize);
        modeIconBox.SizeMode = PictureBoxSizeMode.Zoom;
        modeIconBox.TabStop = false;

        // modeTextLabel
        modeTextLabel.AutoSize = true;
        modeTextLabel.Dock = DockStyle.Fill;
        modeTextLabel.Location = new Point(ModeSectionIconSize + 8, 0);
        modeTextLabel.Margin = new Padding(8, 0, 8, 0);
        modeTextLabel.Name = "modeTextLabel";
        modeTextLabel.Size = new Size(38, 15);
        modeTextLabel.Text = "Mode";
        modeTextLabel.TextAlign = ContentAlignment.MiddleLeft;

        // cpuTempLabel
        cpuTempLabel.AutoSize = true;
        cpuTempLabel.Dock = DockStyle.Fill;
        cpuTempLabel.Location = Point.Empty;
        cpuTempLabel.Margin = new Padding(8, 0, 0, 0);
        cpuTempLabel.Name = "cpuTempLabel";
        cpuTempLabel.Size = new Size(50, 15);
        cpuTempLabel.Text = "CPU: --";
        cpuTempLabel.TextAlign = ContentAlignment.MiddleRight;

        modeTitleTable.Controls.Add(modeIconBox, 0, 0);
        modeTitleTable.Controls.Add(modeTextLabel, 1, 0);
        modeTitleTable.Controls.Add(cpuTempLabel, 2, 0);
        modeTitleTable.ResumeLayout(false);
        modeTitleTable.PerformLayout();

        // modeButtonsTable
        modeButtonsTable.Dock = DockStyle.Fill;
        modeButtonsTable.Location = Point.Empty;
        modeButtonsTable.Margin = new Padding(0);
        modeButtonsTable.Name = "modeButtonsTable";
        modeButtonsTable.Size = new Size(InnerW, ModeTableRowHeight);

        modeLayout.Controls.Add(modeTitleTable, 0, 0);
        modeLayout.Controls.Add(modeButtonsTable, 0, 1);
        modeLayout.ResumeLayout(false);
        modeLayout.PerformLayout();

        modePanel.Controls.Add(modeLayout);
        modePanel.ResumeLayout(false);
        modePanel.PerformLayout();

        // gpuPanel
        gpuPanel.SuspendLayout();
        gpuPanel.Dock = DockStyle.Top;
        gpuPanel.Location = new Point(0, 0);
        gpuPanel.Margin = new Padding(0, 16, 0, 0);
        gpuPanel.Padding = new Padding(20, 0, 20, 0);
        gpuPanel.Name = "gpuPanel";
        gpuPanel.Size = new Size(FormW, StatRowHeight);

        // gpuTable
        gpuTable.SuspendLayout();
        gpuTable.ColumnCount = 2;
        gpuTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StatLabelWidth));
        gpuTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gpuTable.Dock = DockStyle.Fill;
        gpuTable.Location = new Point(20, 0);
        gpuTable.Margin = new Padding(0);
        gpuTable.Name = "gpuTable";
        gpuTable.RowCount = 1;
        gpuTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        gpuTable.Size = new Size(InnerW, StatRowHeight);

        // gpuTitleLabel
        gpuTitleLabel.Dock = DockStyle.Fill;
        gpuTitleLabel.Location = new Point(0, 0);
        gpuTitleLabel.Margin = new Padding(0);
        gpuTitleLabel.Name = "gpuTitleLabel";
        gpuTitleLabel.Padding = new Padding(StatLabelPadLeft, 0, 0, 0);
        gpuTitleLabel.Size = new Size(StatLabelWidth, StatRowHeight);
        gpuTitleLabel.Text = "GPU";
        gpuTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        // gpuTempLabel
        gpuTempLabel.Dock = DockStyle.Fill;
        gpuTempLabel.Location = new Point(StatLabelWidth, 0);
        gpuTempLabel.Margin = new Padding(0);
        gpuTempLabel.Name = "gpuTempLabel";
        gpuTempLabel.Padding = new Padding(0, 0, StatValuePadRight, 0);
        gpuTempLabel.Size = new Size(InnerW - StatLabelWidth, StatRowHeight);
        gpuTempLabel.Text = "--";
        gpuTempLabel.TextAlign = ContentAlignment.MiddleRight;

        gpuTable.Controls.Add(gpuTitleLabel, 0, 0);
        gpuTable.Controls.Add(gpuTempLabel, 1, 0);
        gpuTable.ResumeLayout(false);

        gpuPanel.Controls.Add(gpuTable);
        gpuPanel.ResumeLayout(false);

        // ramPanel
        ramPanel.SuspendLayout();
        ramPanel.Dock = DockStyle.Top;
        ramPanel.Location = new Point(0, 0);
        ramPanel.Margin = new Padding(0, 16, 0, 0);
        ramPanel.Padding = new Padding(20, 0, 20, 0);
        ramPanel.Name = "ramPanel";
        ramPanel.Size = new Size(FormW, StatRowHeight);

        // ramTable
        ramTable.SuspendLayout();
        ramTable.ColumnCount = 2;
        ramTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StatLabelWidth));
        ramTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        ramTable.Dock = DockStyle.Fill;
        ramTable.Location = new Point(20, 0);
        ramTable.Margin = new Padding(0);
        ramTable.Name = "ramTable";
        ramTable.RowCount = 1;
        ramTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ramTable.Size = new Size(InnerW, StatRowHeight);

        // ramTitleLabel
        ramTitleLabel.Dock = DockStyle.Fill;
        ramTitleLabel.Location = new Point(0, 0);
        ramTitleLabel.Margin = new Padding(0);
        ramTitleLabel.Name = "ramTitleLabel";
        ramTitleLabel.Padding = new Padding(StatLabelPadLeft, 0, 0, 0);
        ramTitleLabel.Size = new Size(StatLabelWidth, StatRowHeight);
        ramTitleLabel.Text = "RAM";
        ramTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

        // ramValueLabel
        ramValueLabel.Dock = DockStyle.Fill;
        ramValueLabel.Location = new Point(StatLabelWidth, 0);
        ramValueLabel.Margin = new Padding(0);
        ramValueLabel.Name = "ramValueLabel";
        ramValueLabel.Padding = new Padding(0, 0, StatValuePadRight, 0);
        ramValueLabel.Size = new Size(InnerW - StatLabelWidth, StatRowHeight);
        ramValueLabel.Text = "--";
        ramValueLabel.TextAlign = ContentAlignment.MiddleRight;

        ramTable.Controls.Add(ramTitleLabel, 0, 0);
        ramTable.Controls.Add(ramValueLabel, 1, 0);
        ramTable.ResumeLayout(false);

        ramPanel.Controls.Add(ramTable);
        ramPanel.ResumeLayout(false);

        // startupCheckBox
        startupCheckBox.Dock = DockStyle.Top;
        startupCheckBox.Location = new Point(20, 0);
        startupCheckBox.Margin = new Padding(20, 16, 20, 0);
        startupCheckBox.Name = "startupCheckBox";
        startupCheckBox.Size = new Size(InnerW, StartupRowHeight);
        startupCheckBox.Text = "Run at startup";
        startupCheckBox.UseVisualStyleBackColor = false;

        // Add to contentLayout
        contentLayout.Controls.Add(modePanel, 0, 0);
        contentLayout.Controls.Add(gpuPanel, 0, 1);
        contentLayout.Controls.Add(ramPanel, 0, 2);
        contentLayout.Controls.Add(hotkeyPanel, 0, 3);
        contentLayout.Controls.Add(startupCheckBox, 0, 4);

        contentLayout.ResumeLayout(false);
        contentLayout.PerformLayout();

        mainPanel.Controls.Add(contentLayout);
        mainPanel.ResumeLayout(false);
        mainPanel.PerformLayout();

        Controls.Add(mainPanel);
        Controls.Add(headerPanel);

        ResumeLayout(false);
        PerformLayout();
    }

    private void ConfigureResponsiveLayout()
    {
        headerPanel.Height = ScaleDpi(HeaderHeight);
        headerPanel.Padding = new Padding(ScaleDpi(20), 0, ScaleDpi(12), 0);
        closeButton.Width = ScaleDpi(72);

        mainPanel.Padding = new Padding(0, ScaleDpi(FormPadTop), 0, ScaleDpi(FormPadBottom));

        modePanel.Height = ScaleDpi(ModeLabelH + ModeLabelButtonGap + ModeTableRowHeight + ModePanelBottomPad);
        modePanel.Padding = new Padding(ScaleDpi(20), 0, ScaleDpi(20), 0);

        modeLayout.RowStyles.Clear();
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ScaleDpi(ModeLabelH + ModeLabelButtonGap)));
        modeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ScaleDpi(ModeTableRowHeight)));

        modeTitleTable.ColumnStyles.Clear();
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScaleDpi(ModeSectionIconSize)));
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        modeTitleTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        modeTitleTable.RowStyles.Clear();
        modeTitleTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        modeIconBox.Size = new Size(ScaleDpi(ModeSectionIconSize), ScaleDpi(ModeSectionIconSize));
        modeIconBox.Margin = Padding.Empty;
        modeTextLabel.Margin = new Padding(ScaleDpi(8), 0, ScaleDpi(8), 0);

        cpuTempLabel.Margin = new Padding(ScaleDpi(8), 0, 0, 0);

        ConfigureStatRow(gpuPanel, gpuTable, gpuTitleLabel, gpuTempLabel);
        ConfigureStatRow(ramPanel, ramTable, ramTitleLabel, ramValueLabel);

        startupCheckBox.Height = ScaleDpi(StartupRowHeight);
        startupCheckBox.Margin = new Padding(ScaleDpi(20), ScaleDpi(16), ScaleDpi(20), 0);
    }

    private void ConfigureStatRow(Panel panel, TableLayoutPanel table, Label titleLabel, Label valueLabel)
    {
        panel.Height = ScaleDpi(StatRowHeight);
        panel.Padding = new Padding(ScaleDpi(20), 0, ScaleDpi(20), 0);
        panel.Margin = new Padding(0, ScaleDpi(16), 0, 0);
        panel.Dock = DockStyle.Top;

        table.ColumnStyles.Clear();
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScaleDpi(StatLabelWidth)));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.RowStyles.Clear();
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        titleLabel.Padding = new Padding(ScaleDpi(StatLabelPadLeft), 0, 0, 0);
        valueLabel.Padding = new Padding(0, 0, ScaleDpi(StatValuePadRight), 0);
    }

    private void ApplyTheme()
    {
        BackColor = TrayTheme.Background;
        ForeColor = TrayTheme.Text;

        headerPanel.BackColor = TrayTheme.Background;
        titleLabel.BackColor = TrayTheme.Background;
        titleLabel.ForeColor = TrayTheme.Text;
        titleLabel.Font = TrayTheme.Title;

        closeButton.BackColor = TrayTheme.Background;
        closeButton.ForeColor = TrayTheme.Text;
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = TrayTheme.Background;
        closeButton.FlatAppearance.MouseDownBackColor = TrayTheme.Background;

        mainPanel.BackColor = TrayTheme.Background;
        contentLayout.BackColor = TrayTheme.Background;
        modePanel.BackColor = TrayTheme.Background;
        modeLayout.BackColor = TrayTheme.Background;
        modeTitleTable.BackColor = TrayTheme.Background;
        modeIconBox.BackColor = TrayTheme.Background;
        modeTextLabel.BackColor = TrayTheme.Background;
        modeTextLabel.ForeColor = TrayTheme.Text;
        modeTextLabel.Font = TrayTheme.SectionLabel;
        modeButtonsTable.BackColor = TrayTheme.Background;

        cpuTempLabel.BackColor = TrayTheme.Background;
        cpuTempLabel.ForeColor = TrayTheme.Text;
        cpuTempLabel.Font = TrayTheme.Body; // Match buttons font

        ApplyStatTheme(gpuPanel, gpuTable, gpuTitleLabel, gpuTempLabel);
        ApplyStatTheme(ramPanel, ramTable, ramTitleLabel, ramValueLabel);

        startupCheckBox.BackColor = TrayTheme.Background;
        startupCheckBox.ForeColor = TrayTheme.Text;
        startupCheckBox.Font = TrayTheme.Body;
    }

    private static void ApplyStatTheme(Panel panel, TableLayoutPanel table, Label titleLabel, Label valueLabel)
    {
        panel.BackColor = TrayTheme.Background;
        table.BackColor = TrayTheme.Background;

        titleLabel.BackColor = TrayTheme.Background;
        titleLabel.ForeColor = TrayTheme.Text;
        titleLabel.Font = TrayTheme.SectionLabel;

        valueLabel.BackColor = TrayTheme.Background;
        valueLabel.ForeColor = TrayTheme.Text;
        valueLabel.Font = TrayTheme.Body; // Match buttons font
    }

    private void BuildModeButtons()
    {
        var profiles = PerformanceProfile.All;

        _modeButtons.Clear();
        modeButtonsTable.SuspendLayout();
        foreach (var control in modeButtonsTable.Controls.Cast<Control>().ToArray())
            control.Dispose();

        modeButtonsTable.Controls.Clear();
        var runtimeTable = new TableLayoutPanel
        {
            ColumnCount = profiles.Count,
            RowCount = 1,
            Dock = DockStyle.Fill,
            BackColor = TrayTheme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        runtimeTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        for (var i = 0; i < profiles.Count; i++)
            runtimeTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / profiles.Count));

        var scaledMargin = ScaleDpi(ModeButtonMargin);

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            var button = new ModeButton(profile.Kind, profile.Name, LoadModeButtonIcon(profile.Kind))
            {
                Margin = new Padding(
                    i == 0 ? 0 : scaledMargin,
                    scaledMargin,
                    i == profiles.Count - 1 ? 0 : scaledMargin,
                    scaledMargin)
            };

            button.Click += (_, _) =>
            {
                HighlightModeButton(profile.Kind);
                ModeChangeRequested?.Invoke(this, profile.Kind);
            };

            _modeButtons.Add(button);
            runtimeTable.Controls.Add(button, i, 0);
        }

        modeButtonsTable.Controls.Add(runtimeTable);
        modeButtonsTable.ResumeLayout();
    }

    private async void ConfigureStartupCheck()
    {
        startupCheckBox.CheckedChanged += OnStartupCheckChanged;

        // schtasks.exe query is slow; run it off the UI thread and update the checkbox afterwards.
        var enabled = await Task.Run(StartupHelper.IsEnabled);
        if (IsDisposed)
            return;

        _startupCheckUpdating = true;
        startupCheckBox.Checked = enabled;
        _startupCheckUpdating = false;
    }

    private void WireEvents()
    {
        closeButton.Click += (_, _) => HideAll();

        headerPanel.Paint += OnHeaderPaint;
        gpuPanel.Paint += OnBorderPanelPaint;
        ramPanel.Paint += OnBorderPanelPaint;

        headerPanel.MouseDown += OnDragStart;
        headerPanel.MouseMove += OnDragMove;
        headerPanel.MouseUp += OnDragEnd;
        titleLabel.MouseDown += OnDragStart;
        titleLabel.MouseMove += OnDragMove;
        titleLabel.MouseUp += OnDragEnd;
        MouseDown += OnDragStart;
        MouseMove += OnDragMove;
        MouseUp += OnDragEnd;

        hotkeyButton.Click += (_, _) => BeginHotkeyCapture();

        KeyPreview = true;
        KeyDown += OnFormKeyDown;
        FormClosing += OnFormClosing;
    }

    // Keep the hotkey button in sync with Settings.
    public void RefreshSettingsView()
    {
        RefreshHotkeyText();
    }

    private void BuildHotkeySection()
    {
        hotkeyPanel.Dock = DockStyle.Top;
        hotkeyPanel.AutoSize = true;
        hotkeyPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        hotkeyPanel.WrapContents = true;
        hotkeyPanel.Margin = new Padding(ScaleDpi(20), ScaleDpi(16), ScaleDpi(20), 0);
        hotkeyPanel.Padding = Padding.Empty;
        hotkeyPanel.BackColor = TrayTheme.Background;

        hotkeyLabel.AutoSize = true;
        hotkeyLabel.Margin = new Padding(0, ScaleDpi(8), ScaleDpi(8), 0);
        hotkeyLabel.BackColor = TrayTheme.Background;
        hotkeyLabel.ForeColor = TrayTheme.Text;
        hotkeyLabel.Font = TrayTheme.Body;
        hotkeyLabel.Text = "Mode key: Fn+F10 or";

        hotkeyButton.FlatStyle = FlatStyle.Flat;
        hotkeyButton.FlatAppearance.BorderColor = TrayTheme.ModeButtonBorder;
        hotkeyButton.BackColor = TrayTheme.Surface;
        hotkeyButton.ForeColor = TrayTheme.Text;
        hotkeyButton.Font = TrayTheme.Body;
        // Size to the text so long labels such as "Bellator key (Fn+F10)" are never clipped.
        hotkeyButton.AutoSize = true;
        hotkeyButton.Padding = new Padding(ScaleDpi(10), 0, ScaleDpi(10), 0);
        hotkeyButton.Margin = Padding.Empty;
        hotkeyButton.Cursor = Cursors.Hand;
        hotkeyButton.TabStop = false;

        hotkeyPanel.Controls.Add(hotkeyLabel);
        hotkeyPanel.Controls.Add(hotkeyButton);
    }

    private void BeginHotkeyCapture()
    {
        _capturingHotkey = true;
        RefreshHotkeyText();
    }

    // Win32 MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004.
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingHotkey) return;

        e.SuppressKeyPress = true;

        if (e.KeyCode == Keys.Escape)
        {
            _capturingHotkey = false;
            RefreshHotkeyText();
            return;
        }

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            return;

        var modifiers = 0;
        if (e.Alt) modifiers |= 0x0001;
        if (e.Control) modifiers |= 0x0002;
        if (e.Shift) modifiers |= 0x0004;

        // Require a modifier so a bare key (like P) is not captured system-wide.
        if (modifiers == 0) return;

        _capturingHotkey = false;
        HotkeyChangeRequested?.Invoke(this, new HotkeyRequest(modifiers, (int)e.KeyCode));
    }

    private void RefreshHotkeyText()
    {
        hotkeyButton.Text = _capturingHotkey
            ? "Press keys... (Esc to cancel)"
            : FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey);
    }

    private static string FormatHotkey(int modifiers, int key)
    {
        var parts = new List<string>();
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        parts.Add(key == 0xFF ? "Bellator key (Fn+F10)" : ((Keys)key).ToString());
        return string.Join(" + ", parts);
    }

    private void OnStartupCheckChanged(object? sender, EventArgs e)
    {
        if (_startupCheckUpdating) return;

        var ok = startupCheckBox.Checked ? StartupHelper.Enable() : StartupHelper.Disable();
        if (ok) return;

        _startupCheckUpdating = true;
        startupCheckBox.Checked = !startupCheckBox.Checked;
        _startupCheckUpdating = false;

        MessageBox.Show(
            "Could not update startup settings.",
            AppBranding.FullName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void OnHeaderPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control control) return;

        using var pen = new Pen(TrayTheme.Border, 1);
        e.Graphics.DrawLine(pen, 0, control.Height - 1, control.Width, control.Height - 1);
    }

    private void OnBorderPanelPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control control) return;

        using var pen = new Pen(TrayTheme.Border, 1);
        e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
    }


    private Image? LoadModeButtonIcon(PowerModeKind kind)
    {
        var file = kind switch
        {
            PowerModeKind.Silent => "gauge_left_32.png",
            PowerModeKind.Balanced => "gauge_balanced_32.png",
            PowerModeKind.Beast => "gauge_up_32.png",
            PowerModeKind.Battle => "gauge_right_32.png",
            _ => null
        };
        if (file is null) return null;

        return ResourceImageHelper.Load(file);
    }

    private static Image ScaleModeIcon(Image source, int size)
    {
        return ScaleImage(source, size, size);
    }

    private static Image ScaleImage(Image source, int width, int height)
    {
        var scaled = new Bitmap(width, height);
        using var g = Graphics.FromImage(scaled);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawImage(source, 0, 0, width, height);
        return scaled;
    }

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
        foreach (var button in _modeButtons)
            button.Selected = button.Kind == mode;
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
        private int ScaleDpi(int value) => (int)Math.Round(value * DeviceDpi / 192.0);

        private int ScaledSelectedBorderWidth => ScaleDpi(4);
        private int ScaledContentPaddingX => ScaleDpi(12);
        private int ScaledContentPaddingY => ScaleDpi(1);
        private int ScaledIconTextGap => ScaleDpi(6);

        private bool _selected;
        private readonly Color _borderColor;
        private readonly Image? _icon;
        private readonly string _label;

        public PowerModeKind Kind { get; }

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
            }
        }

        public ModeButton(PowerModeKind kind, string label, Image? icon)
        {
            Kind = kind;
            _borderColor = TrayTheme.ModeFill(kind);
            _icon = icon;
            _label = label;

            Text = label;
            AccessibleName = label;
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = TrayTheme.Surface;
            ForeColor = TrayTheme.ModeForeColor(kind);
            Font = TrayTheme.Body;
            Cursor = Cursors.Hand;
            TabStop = false;
            Dock = DockStyle.Fill;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _icon?.Dispose();

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var rect = ClientRectangle;
            var border = ScaledSelectedBorderWidth;
            var radius = ScaleDpi(ModeButtonBorderRadius);
            var borderDrawColor = _selected ? _borderColor : Color.Transparent;
            var surfaceColor = Parent?.BackColor ?? TrayTheme.Background;
            var contentRect = Rectangle.Inflate(rect, -border, -border);

            using var pathSurface = GetRoundedPath(rect, radius + border);
            using var pathFill = GetRoundedPath(contentRect, radius);
            using var pathBorder = GetRoundedPath(contentRect, radius);
            using var penSurface = new Pen(surfaceColor, border);
            using var penBorder = new Pen(borderDrawColor, border) { Alignment = PenAlignment.Outset };
            using var fillBrush = new SolidBrush(BackColor);

            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Region = new Region(pathSurface);
            pevent.Graphics.Clear(surfaceColor);
            pevent.Graphics.FillPath(fillBrush, pathFill);
            pevent.Graphics.DrawPath(penSurface, pathSurface);
            pevent.Graphics.DrawPath(penBorder, pathBorder);

            DrawContent(pevent.Graphics, contentRect);
        }

        private void DrawContent(Graphics graphics, Rectangle bounds)
        {
            var padX = ScaledContentPaddingX;
            var padY = ScaledContentPaddingY;
            var padded = new Rectangle(bounds.X + padX, bounds.Y + padY, bounds.Width - padX * 2, bounds.Height - padY * 2);
            if (padded.Width <= 0 || padded.Height <= 0) return;

            var textSize = TextRenderer.MeasureText(
                graphics,
                _label,
                Font,
                new Size(padded.Width, int.MaxValue),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            var iconWidth = ScaleDpi(40);
            var iconHeight = ScaleDpi(40);
            var totalHeight = (_icon is null ? 0 : iconHeight) + ScaledIconTextGap + textSize.Height;
            var top = padded.Top + Math.Max(0, (padded.Height - totalHeight) / 2);

            if (_icon is not null)
            {
                var iconX = padded.Left + Math.Max(0, (padded.Width - iconWidth) / 2);
                using var attribs = new System.Drawing.Imaging.ImageAttributes();
                var matrix = new System.Drawing.Imaging.ColorMatrix
                {
                    Matrix40 = ForeColor.R / 255f,
                    Matrix41 = ForeColor.G / 255f,
                    Matrix42 = ForeColor.B / 255f
                };
                attribs.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                graphics.DrawImage(
                    _icon,
                    new Rectangle(iconX, top, iconWidth, iconHeight),
                    0, 0, _icon.Width, _icon.Height,
                    GraphicsUnit.Pixel,
                    attribs);

                top += iconHeight + ScaledIconTextGap;
            }

            var textRect = new Rectangle(padded.Left, top, padded.Width, textSize.Height);
            TextRenderer.DrawText(
                graphics,
                _label,
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.NoPadding);
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
        var path = new GraphicsPath();
        path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
        path.AddArc(w - radius * 2, 0, radius * 2, radius * 2, 270, 90);
        path.AddArc(w - radius * 2, h - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(0, h - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }
}

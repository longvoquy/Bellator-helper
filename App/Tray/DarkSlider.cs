using System.Drawing.Drawing2D;

namespace BHelper.App.Tray;

// Slider painted by hand: the stock TrackBar draws a light control that clashes with the dark theme.
internal sealed class DarkSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public DarkSlider()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick,
            true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Height = 28;
    }

    public event EventHandler? ValueChanged;

    public int Minimum => _minimum;
    public int Maximum => _maximum;

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
                return;

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void SetRange(int minimum, int maximum, int value)
    {
        _minimum = minimum;
        _maximum = Math.Max(minimum, maximum);
        _value = Math.Clamp(value, _minimum, _maximum);
        Invalidate();
    }

    private float DpiScale => DeviceDpi / 96f;
    private int ThumbSize => (int)Math.Round(16 * DpiScale);
    private int TrackHeight => Math.Max(4, (int)Math.Round(5 * DpiScale));

    private float ValueToX(int value)
    {
        var left = ThumbSize / 2f;
        var width = Math.Max(1, Width - ThumbSize);
        var span = Math.Max(1, _maximum - _minimum);
        return left + width * (value - _minimum) / span;
    }

    private int XToValue(int x)
    {
        var left = ThumbSize / 2f;
        var width = Math.Max(1, Width - ThumbSize);
        var ratio = Math.Clamp((x - left) / width, 0f, 1f);
        return _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? TrayTheme.Background);

        var centerY = Height / 2f;
        var thumbX = ValueToX(_value);
        var trackLeft = ThumbSize / 2f;
        var trackRight = Width - ThumbSize / 2f;

        var trackColor = Enabled ? TrayTheme.ModeButtonBorder : Color.FromArgb(30, 38, 52);
        var fillColor = Enabled ? TrayTheme.ModeButtonSelectedBorder : Color.FromArgb(70, 78, 92);
        var thumbColor = Enabled ? TrayTheme.Text : Color.FromArgb(120, 128, 142);

        using (var track = new Pen(trackColor, TrackHeight) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            graphics.DrawLine(track, trackLeft, centerY, trackRight, centerY);

        using (var fill = new Pen(fillColor, TrackHeight) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            graphics.DrawLine(fill, trackLeft, centerY, thumbX, centerY);

        var radius = ThumbSize / 2f;
        using var thumb = new SolidBrush(thumbColor);
        graphics.FillEllipse(thumb, thumbX - radius, centerY - radius, ThumbSize, ThumbSize);

        if (Focused && Enabled)
        {
            using var focus = new Pen(TrayTheme.TooltipTitle, 1f);
            graphics.DrawEllipse(focus, thumbX - radius - 2, centerY - radius - 2, ThumbSize + 4, ThumbSize + 4);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled)
            return;

        Focus();
        _dragging = true;
        Capture = true;
        Value = XToValue(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            Value = XToValue(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled)
            return;

        if (e.KeyCode == Keys.Left)
            Value--;
        else if (e.KeyCode == Keys.Right)
            Value++;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
}

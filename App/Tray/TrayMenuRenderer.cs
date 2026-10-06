using System.Drawing.Drawing2D;

namespace BHelper.App.Tray;

// Dark renderer for the tray context menu so it matches the dashboard instead of the default WinForms look.
internal sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
{
    private const int ItemInset = 4;
    private const int ItemRadius = 6;

    public TrayMenuRenderer()
        : base(new TrayMenuColors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(TrayTheme.Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(TrayTheme.ModeButtonBorder);
        var bounds = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Same color as the menu body: no separate margin strip.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
            return;

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(ItemInset, 1, e.Item.Width - ItemInset * 2, e.Item.Height - 2);
        using var path = RoundedPath(rect, ItemRadius);
        using var brush = new SolidBrush(TrayTheme.Surface);
        graphics.FillPath(brush, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        // Disabled items are the read-only info lines (temperatures, battery): keep them readable.
        e.TextColor = e.Item.Enabled ? TrayTheme.Text : TrayTheme.TooltipTitle;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var box = e.ImageRectangle;
        box.Inflate(-1, -1);
        using (var path = RoundedPath(box, 4))
        using (var fill = new SolidBrush(TrayTheme.Surface))
        using (var border = new Pen(TrayTheme.ModeButtonSelectedBorder, 1.5f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        // White check mark: the default glyph is dark and disappears on a dark menu.
        using var tick = new Pen(TrayTheme.Text, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var x = box.X + box.Width / 2f;
        var y = box.Y + box.Height / 2f;
        graphics.DrawLines(tick,
        [
            new PointF(x - box.Width * 0.22f, y),
            new PointF(x - box.Width * 0.05f, y + box.Height * 0.2f),
            new PointF(x + box.Width * 0.25f, y - box.Height * 0.2f)
        ]);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(TrayTheme.ModeButtonBorder);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, ItemInset * 2, y, e.Item.Width - ItemInset * 2, y);
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class TrayMenuColors : ProfessionalColorTable
    {
        public TrayMenuColors()
        {
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => TrayTheme.Background;
        public override Color ImageMarginGradientBegin => TrayTheme.Background;
        public override Color ImageMarginGradientMiddle => TrayTheme.Background;
        public override Color ImageMarginGradientEnd => TrayTheme.Background;
        public override Color MenuBorder => TrayTheme.ModeButtonBorder;
        public override Color MenuItemBorder => TrayTheme.Surface;
        public override Color MenuItemSelected => TrayTheme.Surface;
        public override Color SeparatorDark => TrayTheme.ModeButtonBorder;
        public override Color SeparatorLight => TrayTheme.ModeButtonBorder;
    }
}

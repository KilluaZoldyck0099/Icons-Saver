using IconsSaver.Core.Models;

namespace IconsSaver.App;

/// <summary>
/// Draws a saved layout as a scale model of the desktop: the monitors, and a mark for every icon.
/// </summary>
/// <remarks>
/// A list row saying "218 icons" tells the user nothing about whether it is the layout they want.
/// Seeing the shape of it — a dense left column, a cluster on the second screen — is what makes
/// layouts recognisable at a glance.
/// </remarks>
internal sealed class DesktopPreview : Control
{
    private static readonly Color MonitorFill = Color.FromArgb(255, 32, 38, 48);
    private static readonly Color MonitorEdge = Color.FromArgb(255, 78, 88, 105);
    private static readonly Color PrimaryEdge = Color.FromArgb(255, 120, 160, 235);
    private static readonly Color IconMark = Color.FromArgb(255, 246, 200, 88);

    private LayoutSnapshot? _snapshot;

    public DesktopPreview()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(255, 18, 21, 27);
        MinimumSize = new Size(120, 80);
    }

    /// <summary>The layout being drawn. Setting it repaints.</summary>
    public LayoutSnapshot? Snapshot
    {
        get => _snapshot;
        set
        {
            _snapshot = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);

        if (_snapshot is null || _snapshot.Display.VirtualWidth <= 0 || _snapshot.Display.VirtualHeight <= 0)
        {
            DrawPlaceholder(graphics);
            return;
        }

        DisplayFingerprint display = _snapshot.Display;

        const int Padding = 10;
        float available = Math.Min(Width - (Padding * 2f), Height - (Padding * 2f));
        if (available <= 0)
        {
            return;
        }

        float scale = Math.Min(
            (Width - (Padding * 2f)) / display.VirtualWidth,
            (Height - (Padding * 2f)) / display.VirtualHeight);

        float offsetX = (Width - (display.VirtualWidth * scale)) / 2f;
        float offsetY = (Height - (display.VirtualHeight * scale)) / 2f;

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        foreach (MonitorDescriptor monitor in display.Monitors)
        {
            // Monitor bounds are in screen coordinates; icon positions are relative to the top-left
            // of the virtual screen, so the monitors have to be shifted into the same space.
            var bounds = new RectangleF(
                offsetX + ((monitor.Left - display.VirtualLeft) * scale),
                offsetY + ((monitor.Top - display.VirtualTop) * scale),
                monitor.Width * scale,
                monitor.Height * scale);

            using var fill = new SolidBrush(MonitorFill);
            using var edge = new Pen(monitor.IsPrimary ? PrimaryEdge : MonitorEdge, monitor.IsPrimary ? 2f : 1f);

            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(edge, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        float markSize = Math.Max(2f, 26f * scale);
        using var iconBrush = new SolidBrush(IconMark);

        foreach (IconEntry icon in _snapshot.Icons)
        {
            graphics.FillRectangle(
                iconBrush,
                offsetX + (icon.X * scale),
                offsetY + (icon.Y * scale),
                markSize,
                markSize);
        }
    }

    private void DrawPlaceholder(Graphics graphics)
    {
        using var brush = new SolidBrush(Color.FromArgb(255, 110, 118, 132));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        graphics.DrawString("Select a layout to preview it", Font, brush, ClientRectangle, format);
    }
}

using IconsSaver.Core.Models;

namespace IconsSaver.App;

/// <summary>
/// Draws a saved layout as a scale model of the desktop, and lets the user pick part of it.
/// </summary>
/// <remarks>
/// <para>
/// A list row saying "218 icons" tells the user nothing about whether it is the layout they want.
/// Seeing the shape of it — a dense left column, a cluster on the second screen — is what makes
/// layouts recognisable at a glance.
/// </para>
/// <para>
/// It doubles as the selector for a partial restore. Clicking a monitor picks that monitor;
/// dragging picks a free rectangle. Both are expressed in the coordinate space of the snapshot,
/// which is the space being drawn, so the selection means exactly what it looks like.
/// </para>
/// </remarks>
internal sealed class DesktopPreview : Control
{
    private const int EdgeInset = 10;

    /// <summary>Movement below this many pixels is a click, not a drag.</summary>
    private const int DragThreshold = 4;

    private static readonly Color MonitorFill = Color.FromArgb(255, 32, 38, 48);
    private static readonly Color MonitorEdge = Color.FromArgb(255, 78, 88, 105);
    private static readonly Color PrimaryEdge = Color.FromArgb(255, 120, 160, 235);
    private static readonly Color IconMark = Color.FromArgb(255, 246, 200, 88);
    private static readonly Color IconMarkDimmed = Color.FromArgb(255, 92, 84, 62);
    private static readonly Color SelectionEdge = Color.FromArgb(255, 120, 190, 255);
    private static readonly Color SelectionFill = Color.FromArgb(48, 90, 150, 230);

    private LayoutSnapshot? _snapshot;
    private RestoreScope _selection = RestoreScope.Everything;

    // Set during painting so mouse positions can be mapped back into icon coordinates.
    private IconProjection _projection = IconProjection.None;

    private Point? _dragAnchor;
    private Point? _dragCursor;

    public DesktopPreview()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        // The preview stays dark in both themes: it is a picture of a screen, and a white one
        // would read as a blank page rather than as a desktop.
        BackColor = Theme.PreviewBackground;
        MinimumSize = new Size(120, 80);
        TabStop = true;
        Cursor = Cursors.Cross;
    }

    /// <summary>The layout being drawn. Setting it clears any selection.</summary>
    public LayoutSnapshot? Snapshot
    {
        get => _snapshot;
        set
        {
            _snapshot = value;

            // A region means nothing once the layout under it has changed, so it never carries over.
            SetSelection(RestoreScope.Everything);
            Invalidate();
        }
    }

    /// <summary>The part of the layout the user has picked, or everything.</summary>
    public RestoreScope Selection => _selection;

    /// <summary>Raised whenever the selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>How many icons of the current layout the selection covers.</summary>
    public int SelectedIconCount
    {
        get
        {
            if (_snapshot is null)
            {
                return 0;
            }

            IconRegion? region = _selection.Resolve(_snapshot.Display);
            return region is null
                ? _snapshot.IconCount
                : _snapshot.Icons.Count(i => region.Value.Contains(i.Position));
        }
    }

    /// <summary>Drops back to restoring the whole desktop.</summary>
    public void ClearSelection() => SetSelection(RestoreScope.Everything);

    // ------------------------------------------------------------------- input

    protected override bool IsInputKey(Keys keyData) => keyData == Keys.Escape || base.IsInputKey(keyData);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button == MouseButtons.Right)
        {
            ClearSelection();
            return;
        }

        if (e.Button == MouseButtons.Left && _snapshot is not null)
        {
            _dragAnchor = e.Location;
            _dragCursor = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragAnchor is not null)
        {
            _dragCursor = e.Location;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_dragAnchor is not Point anchor || _snapshot is null)
        {
            _dragAnchor = null;
            _dragCursor = null;
            return;
        }

        Point release = e.Location;
        _dragAnchor = null;
        _dragCursor = null;

        bool dragged =
            Math.Abs(release.X - anchor.X) >= DragThreshold ||
            Math.Abs(release.Y - anchor.Y) >= DragThreshold;

        SetSelection(dragged ? RegionFromDrag(anchor, release) : MonitorUnder(release));
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape)
        {
            ClearSelection();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Turns a drag into a scope, keeping the current one if the drag described nothing.
    /// </summary>
    /// <remarks>
    /// Falling back to the whole desktop would be the wrong way to fail. A stray gesture would then
    /// silently widen what the next Restore touches, and the button that said "Restore 14 icons" a
    /// moment ago would quietly start meaning all two hundred.
    /// </remarks>
    private RestoreScope RegionFromDrag(Point anchor, Point release)
    {
        IconPosition from = ToIconSpace(anchor);
        IconPosition to = ToIconSpace(release);

        IconRegion region = IconRegion.FromCorners(from.X, from.Y, to.X, to.Y);
        return region.IsEmpty ? _selection : RestoreScope.ForRegion(region);
    }

    /// <summary>
    /// The monitor under a point, or the current selection when the point is on the backdrop.
    /// </summary>
    /// <remarks>
    /// The backdrop is a real target: an L-shaped arrangement leaves a sizeable dead corner inside
    /// the drawn area. Clicking it must not widen the scope, for the same reason as above.
    /// </remarks>
    private RestoreScope MonitorUnder(Point point)
    {
        if (_snapshot is null)
        {
            return _selection;
        }

        IconPosition position = ToIconSpace(point);

        foreach (MonitorDescriptor monitor in _snapshot.Display.Monitors)
        {
            if (IconRegion.ForMonitor(_snapshot.Display, monitor).Contains(position))
            {
                return RestoreScope.ForMonitor(_snapshot.Display, monitor);
            }
        }

        return _selection;
    }

    /// <summary>Maps a point on this control back into the snapshot''s icon coordinates.</summary>
    private IconPosition ToIconSpace(Point point) => _projection.ToIcon(point.X, point.Y);

    private void SetSelection(RestoreScope scope)
    {
        if (_selection == scope)
        {
            return;
        }

        _selection = scope;
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ----------------------------------------------------------------- painting

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

        _projection = IconProjection.Fit(display.VirtualWidth, display.VirtualHeight, Width, Height, EdgeInset);

        if (!_projection.IsUsable)
        {
            return;
        }

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        DrawMonitors(graphics, display);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        DrawIcons(graphics, display);
        DrawSelection(graphics, display);
        DrawHint(graphics);
    }

    private void DrawMonitors(Graphics graphics, DisplayFingerprint display)
    {
        foreach (MonitorDescriptor monitor in display.Monitors)
        {
            IconRegion region = IconRegion.ForMonitor(display, monitor);
            RectangleF bounds = ToControlSpace(region);

            using var fill = new SolidBrush(MonitorFill);
            using var edge = new Pen(monitor.IsPrimary ? PrimaryEdge : MonitorEdge, monitor.IsPrimary ? 2f : 1f);

            graphics.FillRectangle(fill, bounds);
            graphics.DrawRectangle(edge, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }

    private void DrawIcons(Graphics graphics, DisplayFingerprint display)
    {
        IconRegion? scope = _selection.Resolve(display);
        float markSize = Math.Max(2f, 26f * _projection.Scale);

        using var included = new SolidBrush(IconMark);
        using var excluded = new SolidBrush(IconMarkDimmed);

        foreach (IconEntry icon in _snapshot!.Icons)
        {
            bool inScope = scope is null || scope.Value.Contains(icon.Position);

            (float x, float y) = _projection.ToSurface(icon.Position);
            graphics.FillRectangle(inScope ? included : excluded, x, y, markSize, markSize);
        }
    }

    private void DrawSelection(Graphics graphics, DisplayFingerprint display)
    {
        RectangleF? bounds = null;

        if (_dragAnchor is Point anchor && _dragCursor is Point cursor)
        {
            bounds = RectangleF.FromLTRB(
                Math.Min(anchor.X, cursor.X),
                Math.Min(anchor.Y, cursor.Y),
                Math.Max(anchor.X, cursor.X),
                Math.Max(anchor.Y, cursor.Y));
        }
        else if (_selection.Resolve(display) is IconRegion region && !region.IsEmpty)
        {
            bounds = ToControlSpace(region);
        }

        if (bounds is not RectangleF rectangle || rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        using var fill = new SolidBrush(SelectionFill);
        using var edge = new Pen(SelectionEdge, 1.5f);

        graphics.FillRectangle(fill, rectangle);
        graphics.DrawRectangle(edge, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// A single line telling the user the picture is clickable.
    /// </summary>
    /// <remarks>Nothing else on screen says so, and a control that looks like an image will be
    /// treated as one.</remarks>
    private void DrawHint(Graphics graphics)
    {
        string hint = _selection.IsEverything
            ? "Click a screen or drag to restore only part of it"
            : $"Restoring {_selection}  -  right-click or Esc to clear";

        TextRenderer.DrawText(
            graphics,
            hint,
            Font,
            new Rectangle(6, Height - 20, Width - 12, 18),
            Color.FromArgb(_selection.IsEverything ? 130 : 210, 200, 210, 225),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private RectangleF ToControlSpace(IconRegion region)
    {
        (float x, float y, float width, float height) = _projection.ToSurface(region);
        return new RectangleF(x, y, width, height);
    }

    private void DrawPlaceholder(Graphics graphics)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        using var brush = new SolidBrush(Color.FromArgb(255, 110, 118, 132));
        graphics.DrawString("Select a layout to preview it", Font, brush, ClientRectangle, format);
    }
}
